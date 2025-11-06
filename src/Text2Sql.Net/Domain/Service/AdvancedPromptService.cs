using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using System.Text;
using Text2Sql.Net.Domain.Interface;
using Text2Sql.Net.Repositories.Text2Sql.DatabaseSchema;

namespace Text2Sql.Net.Domain.Service
{
    /// <summary>
    /// 高级Prompt工程服务
    /// 实现Few-shot Learning、链式思考和个性化Prompt生成
    /// </summary>
    [ServiceDescription(typeof(IAdvancedPromptService), ServiceLifetime.Scoped)]
    public class AdvancedPromptService : IAdvancedPromptService
    {
        private readonly ILogger<AdvancedPromptService> _logger;
        private readonly List<QueryExample> _examplePool;

        public AdvancedPromptService(ILogger<AdvancedPromptService> logger)
        {
            _logger = logger;
            _examplePool = InitializeExamplePool();
        }

        /// <summary>
        /// 创建渐进式复杂度的Few-shot Prompt
        /// </summary>
        /// <param name="userMessage">用户查询</param>
        /// <param name="schemaInfo">Schema信息</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="userProfile">用户画像（可选）</param>
        /// <returns>优化后的Prompt</returns>
        public async Task<string> CreateProgressivePromptAsync(
            string userMessage, 
            string schemaInfo, 
            string dbType, 
            UserProfile userProfile = null)
        {
            return await CreateProgressivePromptWithExamplesAsync(userMessage, schemaInfo, dbType, string.Empty, userProfile);
        }

        /// <summary>
        /// 创建包含问答示例的渐进式复杂度Prompt
        /// </summary>
        /// <param name="userMessage">用户查询</param>
        /// <param name="schemaInfo">Schema信息</param>
        /// <param name="dbType">数据库类型</param>
        /// <param name="examplesPrompt">格式化的问答示例</param>
        /// <param name="userProfile">用户画像（可选）</param>
        /// <returns>优化后的Prompt</returns>
        public async Task<string> CreateProgressivePromptWithExamplesAsync(
            string userMessage, 
            string schemaInfo, 
            string dbType, 
            string examplesPrompt,
            UserProfile? userProfile = null)
        {
            try
            {
                // 1. 选择最佳示例（使用内置示例池）
                var selectedExamples = await SelectBestExamplesAsync(userMessage, schemaInfo, k: 3);

                // 2. 按复杂度排序
                var sortedExamples = selectedExamples.OrderBy(e => e.ComplexityScore).ToList();

                // 3. 生成推理链
                var reasoningChain = await GenerateReasoningChainAsync(userMessage, schemaInfo);

                // 4. 构建渐进式Prompt（包含用户提供的问答示例）
                var prompt = AdvancedPromptService.BuildProgressivePromptWithExamples(userMessage, schemaInfo, dbType, sortedExamples, reasoningChain, examplesPrompt, userProfile);

                _logger.LogInformation($"生成了包含 {selectedExamples.Count} 个内置示例的渐进式Prompt" + 
                    (!string.IsNullOrEmpty(examplesPrompt) ? "，并包含了用户问答示例" : ""));
                return prompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"创建渐进式Prompt时出错：{ex.Message}");
                // 返回基础Prompt作为后备
                return CreateBasicPrompt(userMessage, schemaInfo, dbType);
            }
        }

        /// <summary>
        /// 基于相似度选择最佳示例
        /// </summary>
        /// <param name="userMessage">用户查询</param>
        /// <param name="schemaInfo">Schema信息</param>
        /// <param name="k">返回示例数量</param>
        /// <returns>最佳示例列表</returns>
        private async Task<List<QueryExample>> SelectBestExamplesAsync(string userMessage, string schemaInfo, int k = 3)
        {
            var similarities = new List<(QueryExample example, double similarity)>();

            foreach (var example in _examplePool)
            {
                // 计算查询相似度（这里使用简单的文本相似度，实际应用中可以使用嵌入向量）
                double querySimilarity = CalculateTextSimilarity(userMessage, example.Question);
                
                // 计算Schema相似度
                double schemaSimilarity = CalculateSchemaSimilarity(schemaInfo, example.SchemaContext);
                
                // 综合相似度
                double overallSimilarity = 0.7 * querySimilarity + 0.3 * schemaSimilarity;
                
                similarities.Add((example, overallSimilarity));
            }

            // 选择相似度最高的k个示例
            return similarities
                .OrderByDescending(s => s.similarity)
                .Take(k)
                .Select(s => s.example)
                .ToList();
        }

        /// <summary>
        /// 生成链式思考推理链
        /// </summary>
        /// <param name="userMessage">用户查询</param>
        /// <param name="schemaInfo">Schema信息</param>
        /// <returns>推理链</returns>
        private async Task<ChainOfThoughtSteps> GenerateReasoningChainAsync(string userMessage, string schemaInfo)
        {
            var steps = new ChainOfThoughtSteps();

            // 1. 意图理解
            steps.Intent = AnalyzeIntent(userMessage);

            // 2. 实体识别
            steps.KeyEntities = ExtractEntities(userMessage, schemaInfo);

            // 3. 表选择
            steps.RelevantTables = SelectRelevantTables(steps.KeyEntities, schemaInfo);

            // 4. 关系分析
            steps.TableRelationships = AnalyzeTableRelationships(steps.RelevantTables, schemaInfo);

            // 5. SQL结构规划
            steps.SqlStructure = PlanSqlStructure(steps.Intent, steps.KeyEntities, steps.RelevantTables);

            return steps;
        }

        /// <summary>
        /// 构建渐进式复杂度Prompt
        /// </summary>
        private static string BuildProgressivePrompt(
            string userMessage, 
            string schemaInfo, 
            string dbType, 
            List<QueryExample> examples, 
            ChainOfThoughtSteps reasoning,
            UserProfile userProfile)
        {
            var prompt = new StringBuilder();

            // 1. 角色定义和任务说明
            prompt.AppendLine("# 专业SQL查询生成专家");
            prompt.AppendLine();
            prompt.AppendLine("您是一位资深的SQL查询生成专家，具备深厚的数据库理论基础和丰富的实战经验。");
            prompt.AppendLine("您的任务是将自然语言查询转换为高效、准确的SQL语句。");
            prompt.AppendLine();

            // 2. 数据库信息
            prompt.AppendLine("## 数据库环境");
            prompt.AppendLine($"- **数据库类型**: {dbType}");
            prompt.AppendLine("- **表结构信息**:");
            prompt.AppendLine("```json");
            prompt.AppendLine(schemaInfo);
            prompt.AppendLine("```");
            prompt.AppendLine();

            // 3. 渐进式示例（从简单到复杂）
            prompt.AppendLine("## 查询示例（按复杂度递增）");
            prompt.AppendLine();

            for (int i = 0; i < examples.Count; i++)
            {
                var example = examples[i];
                prompt.AppendLine($"### 示例 {i + 1} - 复杂度: {example.ComplexityScore:F1}");
                prompt.AppendLine($"**问题**: {example.Question}");
                prompt.AppendLine();
                prompt.AppendLine("**分析过程**:");
                foreach (var step in example.ReasoningSteps)
                {
                    prompt.AppendLine($"- {step}");
                }
                prompt.AppendLine();
                prompt.AppendLine("**SQL查询**:");
                prompt.AppendLine("```sql");
                prompt.AppendLine(example.Sql);
                prompt.AppendLine("```");
                prompt.AppendLine();
            }

            // 4. 链式思考指导
            prompt.AppendLine("## 分析方法论");
            prompt.AppendLine();
            prompt.AppendLine("请按照以下步骤进行分析：");
            prompt.AppendLine();
            prompt.AppendLine("### 第一步：意图理解");
            prompt.AppendLine("- 识别查询的主要目的（查询、统计、比较、排序等）");
            prompt.AppendLine("- 确定查询的业务场景和约束条件");
            prompt.AppendLine();
            prompt.AppendLine("### 第二步：实体识别");
            prompt.AppendLine("- 提取查询中的关键实体（表名、列名、值等）");
            prompt.AppendLine("- 识别时间范围、数值条件、文本匹配等约束");
            prompt.AppendLine();
            prompt.AppendLine("### 第三步：表结构分析");
            prompt.AppendLine("- 确定需要查询的主要表");
            prompt.AppendLine("- 分析表间关系和JOIN条件");
            prompt.AppendLine("- 识别主键、外键和索引策略");
            prompt.AppendLine();
            prompt.AppendLine("### 第四步：查询优化");
            prompt.AppendLine("- 选择最优的JOIN策略");
            prompt.AppendLine("- 合理使用WHERE条件过滤");
            prompt.AppendLine("- 考虑查询性能和可读性");
            prompt.AppendLine();

            // 5. 个性化调整
            if (userProfile != null)
            {
                prompt.AppendLine("## 个性化指导");
                
                if (userProfile.ExpertiseLevel < 0.3)
                {
                    prompt.AppendLine("- 请生成简单易懂的SQL，并添加详细注释");
                    prompt.AppendLine("- 优先使用基础语法，避免复杂的子查询");
                }
                else if (userProfile.ExpertiseLevel > 0.7)
                {
                    prompt.AppendLine("- 请生成高效优化的SQL查询");
                    prompt.AppendLine("- 可以使用高级特性如CTE、窗口函数等");
                }

                if (userProfile.PreferVerboseExplanation)
                {
                    prompt.AppendLine("- 请详细解释查询逻辑和每个部分的作用");
                }

                if (userProfile.CommonPatterns.Any())
                {
                    prompt.AppendLine($"- 参考用户常用模式: {string.Join(", ", userProfile.CommonPatterns)}");
                }

                prompt.AppendLine();
            }

            // 6. 当前查询分析
            prompt.AppendLine("## 当前查询分析");
            prompt.AppendLine();
            prompt.AppendLine($"**用户问题**: {userMessage}");
            prompt.AppendLine();
            prompt.AppendLine("**预分析结果**:");
            prompt.AppendLine($"- 查询意图: {reasoning.Intent}");
            prompt.AppendLine($"- 关键实体: {string.Join(", ", reasoning.KeyEntities)}");
            prompt.AppendLine($"- 相关表: {string.Join(", ", reasoning.RelevantTables)}");
            prompt.AppendLine($"- 表关系: {reasoning.TableRelationships}");
            prompt.AppendLine($"- SQL结构: {reasoning.SqlStructure}");
            prompt.AppendLine();

            // 7. 输出要求
            prompt.AppendLine("## 输出要求");
            prompt.AppendLine();
            prompt.AppendLine("请按照以下格式输出：");
            prompt.AppendLine();
            prompt.AppendLine("1. **分析过程** (可选，根据用户偏好):");
            prompt.AppendLine("   - 简要说明您的分析思路");
            prompt.AppendLine();
            prompt.AppendLine("2. **SQL查询**:");
            prompt.AppendLine("   - 生成完整、可执行的SQL语句");
            prompt.AppendLine("   - 确保语法正确，符合指定数据库类型");
            prompt.AppendLine("   - 不要包含任何格式标记（如```sql```）");
            prompt.AppendLine();

            // 8. 质量检查清单
            prompt.AppendLine("## 质量检查清单");
            prompt.AppendLine();
            prompt.AppendLine("生成SQL前请确认：");
            prompt.AppendLine("- [ ] 表名和列名拼写正确");
            prompt.AppendLine("- [ ] JOIN条件准确无误");
            prompt.AppendLine("- [ ] WHERE条件逻辑正确");
            prompt.AppendLine("- [ ] 聚合函数使用恰当");
            prompt.AppendLine("- [ ] 排序和分页符合需求");
            prompt.AppendLine("- [ ] 语法符合目标数据库类型");
            prompt.AppendLine();

            prompt.AppendLine("现在请分析上述查询并生成对应的SQL语句：");

            return prompt.ToString();
        }

        /// <summary>
        /// 构建包含用户问答示例的渐进式Prompt
        /// </summary>
        private static string BuildProgressivePromptWithExamples(
            string userMessage, 
            string schemaInfo, 
            string dbType, 
            List<QueryExample> examples, 
            ChainOfThoughtSteps reasoning,
            string examplesPrompt,
            UserProfile userProfile)
        {
            var prompt = new StringBuilder();

            // 1. 角色定义和任务说明
            prompt.AppendLine("# 专业SQL查询生成专家");
            prompt.AppendLine();
            prompt.AppendLine("您是一位资深的SQL查询生成专家，具备深厚的数据库理论基础和丰富的实战经验。");
            prompt.AppendLine("您的任务是将自然语言查询转换为高效、准确的SQL语句。");
            prompt.AppendLine($"当前时间:{DateTime.Now}");
            prompt.AppendLine();

            // 2. 用户提供的问答示例（最高优先级 - 移到最前面）
            if (!string.IsNullOrEmpty(examplesPrompt))
            {
                prompt.AppendLine("## 【重要】参考示例 - 务必遵循");
                prompt.AppendLine();
                prompt.AppendLine("**以下是与当前查询高度相关的实际问答示例，这些示例来自相同的数据库环境，具有极高的参考价值。**");
                prompt.AppendLine();
                prompt.AppendLine("**请特别注意：**");
                prompt.AppendLine("1. 这些示例展示了在当前数据库中解决类似问题的正确方法");
                prompt.AppendLine("2. 必须严格参考示例中的SQL编写风格、表关联方式和查询模式");
                prompt.AppendLine("3. 示例中使用的表名、字段名、JOIN方式都是针对当前数据库优化过的");
                prompt.AppendLine("4. 优先采用示例中展示的查询结构和技巧");
                prompt.AppendLine();
                prompt.AppendLine("---");
                prompt.AppendLine();
                prompt.AppendLine(examplesPrompt);
                prompt.AppendLine("---");
                prompt.AppendLine();
                prompt.AppendLine("**请基于以上示例的模式和风格来生成当前查询的SQL语句。**");
                prompt.AppendLine();
            }

            // 3. 数据库信息
            prompt.AppendLine("## 数据库环境");
            prompt.AppendLine($"- **数据库类型**: {dbType}");
            prompt.AppendLine("- **表结构信息**:");
            prompt.AppendLine("```json");
            prompt.AppendLine(schemaInfo);
            prompt.AppendLine("```");
            prompt.AppendLine();

            // 4. 内置渐进式示例（从简单到复杂）
            if (examples.Count > 0)
            {
                prompt.AppendLine("## 通用查询示例（按复杂度递增）");
                prompt.AppendLine();
                prompt.AppendLine("以下示例展示了不同复杂度的SQL查询模式：");
                prompt.AppendLine();

                for (int i = 0; i < examples.Count; i++)
                {
                    var example = examples[i];
                    prompt.AppendLine($"### 示例 {i + 1} - 复杂度: {example.ComplexityScore:F1}");
                    prompt.AppendLine($"**问题**: {example.Question}");
                    prompt.AppendLine();
                    prompt.AppendLine("**分析过程**:");
                    foreach (var step in example.ReasoningSteps)
                    {
                        prompt.AppendLine($"- {step}");
                    }
                    prompt.AppendLine();
                    prompt.AppendLine("**SQL查询**:");
                    prompt.AppendLine("```sql");
                    prompt.AppendLine(example.Sql);
                    prompt.AppendLine("```");
                    prompt.AppendLine();
                }
            }

            // 5. 推理指导原则
            prompt.AppendLine("## 分析指导原则");
            prompt.AppendLine();
            prompt.AppendLine("请按照以下步骤进行分析：");
            prompt.AppendLine("1. **理解需求**: 仔细分析用户的查询意图");
            prompt.AppendLine("2. **识别实体**: 找出涉及的业务对象和属性");
            prompt.AppendLine("3. **分析关系**: 确定表之间的关联关系");
            prompt.AppendLine("4. **构建查询**: 设计高效的SQL查询结构");
            prompt.AppendLine("5. **优化性能**: 考虑索引和查询性能");
            prompt.AppendLine();

            // 6. 个性化指导
            if (userProfile != null)
            {
                prompt.AppendLine("## 个性化指导");
                
                if (userProfile.ExpertiseLevel < 0.3)
                {
                    prompt.AppendLine("- 请生成简单易懂的SQL，并添加详细注释");
                    prompt.AppendLine("- 优先使用基础语法，避免复杂的子查询");
                }
                else if (userProfile.ExpertiseLevel > 0.7)
                {
                    prompt.AppendLine("- 请生成高效优化的SQL查询");
                    prompt.AppendLine("- 可以使用高级特性如CTE、窗口函数等");
                }

                if (userProfile.PreferVerboseExplanation)
                {
                    prompt.AppendLine("- 请详细解释查询逻辑和每个部分的作用");
                }

                if (userProfile.CommonPatterns.Any())
                {
                    prompt.AppendLine($"- 参考用户常用模式: {string.Join(", ", userProfile.CommonPatterns)}");
                }

                prompt.AppendLine();
            }

            // 7. 当前查询分析
            prompt.AppendLine("## 当前查询分析");
            prompt.AppendLine();
            prompt.AppendLine($"**用户问题**: {userMessage}");
            prompt.AppendLine();
            prompt.AppendLine("**预分析结果**:");
            prompt.AppendLine($"- 查询意图: {reasoning.Intent}");
            prompt.AppendLine($"- 关键实体: {string.Join(", ", reasoning.KeyEntities)}");
            prompt.AppendLine($"- 相关表: {string.Join(", ", reasoning.RelevantTables)}");
            prompt.AppendLine($"- 表关系: {reasoning.TableRelationships}");
            prompt.AppendLine($"- SQL结构: {reasoning.SqlStructure}");
            prompt.AppendLine();

            // 8. 输出要求
            prompt.AppendLine("## 输出要求");
            prompt.AppendLine();
            if (!string.IsNullOrEmpty(examplesPrompt))
            {
                prompt.AppendLine("**特别提醒：上面提供的参考示例非常重要，请务必：**");
                prompt.AppendLine("- 参考示例中的SQL结构和编写风格");
                prompt.AppendLine("- 使用示例中展示的表关联方式");
                prompt.AppendLine("- 借鉴示例中的查询技巧和模式");
                prompt.AppendLine();
            }
            prompt.AppendLine("请按照以下格式输出：");
            prompt.AppendLine();
            prompt.AppendLine("1. **分析过程** (可选，根据用户偏好):");
            prompt.AppendLine("   - 简要说明您的分析思路");
            prompt.AppendLine();
            prompt.AppendLine("2. **SQL查询**:");
            prompt.AppendLine("   - 生成完整、可执行的SQL语句");
            prompt.AppendLine("   - 确保语法正确，符合指定数据库类型");
            prompt.AppendLine("   - 不要包含任何格式标记（如```sql```）");
            prompt.AppendLine();

            // 9. 质量检查清单
            prompt.AppendLine("## 质量检查清单");
            prompt.AppendLine();
            prompt.AppendLine("生成SQL前请确认：");
            prompt.AppendLine("- [ ] 表名和列名拼写正确");
            prompt.AppendLine("- [ ] JOIN条件准确无误");
            prompt.AppendLine("- [ ] WHERE条件逻辑正确");
            prompt.AppendLine("- [ ] 聚合函数使用恰当");
            prompt.AppendLine("- [ ] 排序和分页符合需求");
            prompt.AppendLine("- [ ] 语法符合目标数据库类型");
            prompt.AppendLine();

            prompt.AppendLine("现在请分析上述查询并生成对应的SQL语句：");

            return prompt.ToString();
        }

        /// <summary>
        /// 创建基础Prompt（后备方案）
        /// </summary>
        private string CreateBasicPrompt(string userMessage, string schemaInfo, string dbType)
        {
            return $@"# SQL查询助手

## 角色定义
您是一位专业的SQL查询生成器，精通将自然语言问题转换为高效、准确的SQL查询语句。

## 输入参数
- 数据库类型：{dbType}
- 数据库表结构：{schemaInfo}
- 用户问题：{userMessage}

## 主要任务
生成一个针对用户问题的可执行SQL查询语句，确保语法正确并符合指定数据库类型的特性。

## 输出格式
纯SQL查询语句，不含任何其他文本或格式标记";
        }

        /// <summary>
        /// 分析查询意图
        /// </summary>
        private string AnalyzeIntent(string userMessage)
        {
            var message = userMessage.ToLower();
            
            if (message.Contains("统计") || message.Contains("count") || message.Contains("数量"))
                return "统计查询";
            if (message.Contains("排序") || message.Contains("最高") || message.Contains("最低") || message.Contains("top"))
                return "排序查询";
            if (message.Contains("分组") || message.Contains("按") || message.Contains("group"))
                return "分组统计";
            if (message.Contains("最近") || message.Contains("时间") || message.Contains("日期"))
                return "时间范围查询";
            if (message.Contains("比较") || message.Contains("对比"))
                return "比较分析";
            
            return "基础查询";
        }

        /// <summary>
        /// 提取关键实体
        /// </summary>
        private List<string> ExtractEntities(string userMessage, string schemaInfo)
        {
            var entities = new List<string>();
            
            // 简单的实体提取逻辑（实际应用中可以使用NLP库）
            var words = userMessage.Split(' ', '，', '。', '、');
            
            foreach (var word in words)
            {
                if (word.Length > 2 && !IsStopWord(word))
                {
                    entities.Add(word.Trim());
                }
            }
            
            return entities.Distinct().ToList();
        }

        /// <summary>
        /// 选择相关表
        /// </summary>
        private List<string> SelectRelevantTables(List<string> entities, string schemaInfo)
        {
            var tables = new List<string>();
            
            try
            {
                var schema = JsonConvert.DeserializeObject<List<TableInfo>>(schemaInfo);
                foreach (var table in schema)
                {
                    foreach (var entity in entities)
                    {
                        if (table.TableName.ToLower().Contains(entity.ToLower()) ||
                            table.Description?.ToLower().Contains(entity.ToLower()) == true)
                        {
                            tables.Add(table.TableName);
                            break;
                        }
                    }
                }
            }
            catch
            {
                // 如果解析失败，返回空列表
            }
            
            return tables.Distinct().ToList();
        }

        /// <summary>
        /// 分析表关系
        /// </summary>
        private string AnalyzeTableRelationships(List<string> tables, string schemaInfo)
        {
            if (tables.Count <= 1)
                return "单表查询";
            
            try
            {
                var schema = JsonConvert.DeserializeObject<List<TableInfo>>(schemaInfo);
                var relationships = new List<string>();
                
                foreach (var table in schema.Where(t => tables.Contains(t.TableName)))
                {
                    foreach (var fk in table.ForeignKeys)
                    {
                        if (tables.Contains(fk.ReferencedTableName))
                        {
                            relationships.Add($"{table.TableName}->{fk.ReferencedTableName}");
                        }
                    }
                }
                
                return relationships.Any() ? string.Join(", ", relationships) : "需要手动关联";
            }
            catch
            {
                return "关系分析失败";
            }
        }

        /// <summary>
        /// 规划SQL结构
        /// </summary>
        private string PlanSqlStructure(string intent, List<string> entities, List<string> tables)
        {
            var structure = new StringBuilder();
            
            structure.Append("SELECT ");
            
            if (intent.Contains("统计"))
                structure.Append("COUNT(*) ");
            else if (intent.Contains("分组"))
                structure.Append("列名, COUNT(*) ");
            else
                structure.Append("* ");
            
            structure.Append($"FROM {string.Join(" JOIN ", tables)} ");
            
            if (entities.Any())
                structure.Append("WHERE 条件 ");
            
            if (intent.Contains("分组"))
                structure.Append("GROUP BY 列名 ");
            
            if (intent.Contains("排序"))
                structure.Append("ORDER BY 列名 ");
            
            return structure.ToString();
        }

        /// <summary>
        /// 计算文本相似度（简单实现）
        /// </summary>
        private double CalculateTextSimilarity(string text1, string text2)
        {
            var words1 = text1.ToLower().Split(' ').ToHashSet();
            var words2 = text2.ToLower().Split(' ').ToHashSet();
            
            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();
            
            return union == 0 ? 0 : (double)intersection / union;
        }

        /// <summary>
        /// 计算Schema相似度
        /// </summary>
        private double CalculateSchemaSimilarity(string schema1, string schema2)
        {
            // 简单实现，实际应用中可以使用更复杂的算法
            return CalculateTextSimilarity(schema1, schema2);
        }

        /// <summary>
        /// 判断是否为停用词
        /// </summary>
        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string> { "的", "是", "在", "有", "和", "与", "或", "但", "及", "以及", "等" };
            return stopWords.Contains(word.ToLower());
        }

        /// <summary>
        /// 初始化示例池
        /// </summary>
        private List<QueryExample> InitializeExamplePool()
        {
            return new List<QueryExample>
            {
                new QueryExample
                {
                    Question = "查询所有用户信息",
                    Sql = "SELECT * FROM users;",
                    ComplexityScore = 1.0,
                    ReasoningSteps = new List<string>
                    {
                        "识别查询目标：获取用户表的所有记录",
                        "确定数据源：users表", 
                        "选择查询类型：简单SELECT查询"
                    },
                    SchemaContext = "包含users表的基础查询"
                },
                new QueryExample
                {
                    Question = "统计每个部门的员工数量",
                    Sql = "SELECT department, COUNT(*) as employee_count FROM employees GROUP BY department;",
                    ComplexityScore = 2.5,
                    ReasoningSteps = new List<string>
                    {
                        "识别查询目标：按部门统计员工数量",
                        "确定聚合方式：使用COUNT函数",
                        "确定分组字段：department",
                        "构建GROUP BY查询"
                    },
                    SchemaContext = "包含employees表，需要分组统计"
                },
                new QueryExample
                {
                    Question = "查询订单金额大于1000的客户及其最近订单信息",
                    Sql = @"SELECT c.customer_name, o.order_date, o.total_amount 
                           FROM customers c 
                           JOIN orders o ON c.customer_id = o.customer_id 
                           WHERE o.total_amount > 1000 
                           ORDER BY o.order_date DESC;",
                    ComplexityScore = 4.0,
                    ReasoningSteps = new List<string>
                    {
                        "识别查询目标：获取高金额订单的客户信息",
                        "分析表关系：customers和orders通过customer_id关联",
                        "确定过滤条件：订单金额大于1000",
                        "确定排序方式：按订单日期降序",
                        "构建JOIN查询"
                    },
                    SchemaContext = "包含customers和orders表的关联查询"
                }
            };
        }
    }

    /// <summary>
    /// 查询示例
    /// </summary>
    public class QueryExample
    {
        public string Question { get; set; }
        public string Sql { get; set; }
        public double ComplexityScore { get; set; }
        public List<string> ReasoningSteps { get; set; } = new List<string>();
        public string SchemaContext { get; set; }
    }

    /// <summary>
    /// 链式思考步骤
    /// </summary>
    public class ChainOfThoughtSteps
    {
        public string Intent { get; set; }
        public List<string> KeyEntities { get; set; } = new List<string>();
        public List<string> RelevantTables { get; set; } = new List<string>();
        public string TableRelationships { get; set; }
        public string SqlStructure { get; set; }
    }

    /// <summary>
    /// 用户画像
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; }
        public double ExpertiseLevel { get; set; } // 0-1, 专业水平
        public bool PreferVerboseExplanation { get; set; } // 是否喜欢详细解释
        public List<string> CommonPatterns { get; set; } = new List<string>(); // 常用查询模式
        public Dictionary<string, int> QueryHistory { get; set; } = new Dictionary<string, int>(); // 查询历史统计
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}

