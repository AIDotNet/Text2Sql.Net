using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Text2Sql.Net.Domain.Interface;
using Text2Sql.Net.Repositories.Text2Sql.QAExample;

namespace Text2Sql.Net.Domain.Service
{
    /// <summary>
    /// 问答示例服务实现
    /// </summary>
    [ServiceDescription(typeof(IQAExampleService), ServiceLifetime.Scoped)]
    public class QAExampleService : IQAExampleService
    {
        private readonly IQAExampleRepository _exampleRepository;
        private readonly ISemanticService _semanticService;
        private readonly ILogger<QAExampleService> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public QAExampleService(
            IQAExampleRepository exampleRepository,
            ISemanticService semanticService,
            ILogger<QAExampleService> logger)
        {
            _exampleRepository = exampleRepository;
            _semanticService = semanticService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<List<QAExample>> GetRelevantExamplesAsync(string connectionId, string userQuestion, int limit = 3, double minRelevanceScore = 0.7)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(userQuestion))
                {
                    return new List<QAExample>();
                }

                // 获取语义内存
                SemanticTextMemory memory = await _semanticService.GetTextMemory();
                
                // 使用"qa_examples_"前缀来区分问答示例的集合
                string collectionName = $"qa_examples_{connectionId}";

                var relevantExamples = new List<QAExample>();
                var searchResults = new List<MemoryQueryResult>();

                // 进行语义搜索
                await foreach (var result in memory.SearchAsync(collectionName, userQuestion, limit: limit * 2, minRelevanceScore: minRelevanceScore))
                {
                    searchResults.Add(result);
                }

                // 解析搜索结果
                foreach (var result in searchResults.Take(limit))
                {
                    try
                    {
                        // 从metadata中解析示例ID
                        var exampleData = JsonConvert.DeserializeObject<QAExampleEmbedding>(result.Metadata.Text);
                        if (exampleData != null)
                        {
                            var example = await _exampleRepository.GetByIdAsync(exampleData.ExampleId);
                            if (example != null && example.IsEnabled)
                            {
                                _logger.LogInformation($"找到相关问答示例: {example.Question}，相关性分数: {result.Relevance:F2}");
                                relevantExamples.Add(example);
                                
                                // 更新使用统计
                                await _exampleRepository.UpdateUsageStatisticsAsync(example.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"解析问答示例搜索结果时出错：{ex.Message}");
                    }
                }

                return relevantExamples;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取相关问答示例时出错：{ex.Message}");
                return new List<QAExample>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> AddExampleAsync(QAExample example)
        {
            try
            {
                if (example == null || string.IsNullOrEmpty(example.Question) || string.IsNullOrEmpty(example.SqlQuery))
                {
                    return false;
                }

                // 保存到数据库
                var success = await _exampleRepository.InsertAsync(example);
                if (!success)
                {
                    return false;
                }

                // 生成向量嵌入
                await GenerateEmbeddingForExampleAsync(example);

                _logger.LogInformation($"成功添加问答示例：{example.Question}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加问答示例时出错：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CreateFromCorrectionAsync(string connectionId, string userQuestion, string correctSql, string incorrectSql = null, string description = null)
        {
            try
            {
                var example = new QAExample
                {
                    ConnectionId = connectionId,
                    Question = userQuestion,
                    SqlQuery = correctSql,
                    Description = description ?? "用户修正生成的示例",
                    Source = ExampleSource.Correction,
                    OriginalIncorrectSql = incorrectSql,
                    Category = "correction",
                    CreatedBy = "system"
                };

                return await AddExampleAsync(example);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"从修正创建问答示例时出错：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateExampleAsync(QAExample example)
        {
            try
            {
                if (example == null)
                {
                    return false;
                }

                example.UpdateTime = DateTime.Now;
                var success = await _exampleRepository.UpdateAsync(example);
                
                if (success)
                {
                    // 重新生成向量嵌入
                    await GenerateEmbeddingForExampleAsync(example);
                    _logger.LogInformation($"成功更新问答示例：{example.Question}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新问答示例时出错：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteExampleAsync(string exampleId)
        {
            try
            {
                var example = await _exampleRepository.GetByIdAsync(exampleId);
                if (example == null)
                {
                    return false;
                }

                // 从向量存储中删除
                await DeleteEmbeddingForExampleAsync(example);

                // 从数据库中删除
                var success = await _exampleRepository.DeleteAsync(exampleId);
                if (success)
                {
                    _logger.LogInformation($"成功删除问答示例：{example.Question}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除问答示例时出错：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<List<QAExample>> GetExamplesByConnectionIdAsync(string connectionId)
        {
            try
            {
                return await _exampleRepository.GetByConnectionIdAsync(connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取问答示例列表时出错：{ex.Message}");
                return new List<QAExample>();
            }
        }

        /// <inheritdoc/>
        public async Task<List<QAExample>> SearchExamplesAsync(string connectionId, string keyword)
        {
            try
            {
                return await _exampleRepository.SearchAsync(connectionId, keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"搜索问答示例时出错：{ex.Message}");
                return new List<QAExample>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> BatchUpdateEnabledAsync(List<string> exampleIds, bool isEnabled)
        {
            try
            {
                return await _exampleRepository.BatchUpdateEnabledAsync(exampleIds, isEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"批量更新问答示例状态时出错：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RegenerateEmbeddingsAsync(string connectionId)
        {
            try
            {
                var examples = await _exampleRepository.GetEnabledByConnectionIdAsync(connectionId);
                
                foreach (var example in examples)
                {
                    await GenerateEmbeddingForExampleAsync(example);
                }

                _logger.LogInformation($"成功为{examples.Count}个问答示例重新生成向量嵌入");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"重新生成向量嵌入时出错：{ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public string FormatExamplesForPrompt(List<QAExample> examples)
        {
            if (examples == null || examples.Count == 0)
            {
                return string.Empty;
            }

            var promptBuilder = new StringBuilder();

            for (int i = 0; i < examples.Count; i++)
            {
                var example = examples[i];
                promptBuilder.AppendLine($"**示例 {i + 1}**:");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"**用户问题**: {example.Question}");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine($"**标准SQL查询**:");
                promptBuilder.AppendLine("```sql");
                promptBuilder.AppendLine(example.SqlQuery);
                promptBuilder.AppendLine("```");
                
                if (!string.IsNullOrEmpty(example.Description))
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine($"**说明**: {example.Description}");
                }
                
                promptBuilder.AppendLine();
            }
            
            return promptBuilder.ToString();
        }

        /// <summary>
        /// 为问答示例生成向量嵌入
        /// </summary>
        /// <param name="example">问答示例</param>
        /// <returns>是否成功</returns>
        private async Task<bool> GenerateEmbeddingForExampleAsync(QAExample example)
        {
            try
            {
                SemanticTextMemory memory = await _semanticService.GetTextMemory();
                string collectionName = $"qa_examples_{example.ConnectionId}";

                // 构建用于嵌入的文本，包含问题和相关描述信息
                var embeddingText = new StringBuilder();
                embeddingText.AppendLine($"问题: {example.Question}");
                
                if (!string.IsNullOrEmpty(example.Description))
                {
                    embeddingText.AppendLine($"描述: {example.Description}");
                }
                
                if (!string.IsNullOrEmpty(example.Category))
                {
                    embeddingText.AppendLine($"分类: {example.Category}");
                }

                // 创建嵌入元数据
                var embeddingData = new QAExampleEmbedding
                {
                    ExampleId = example.Id,
                    Question = example.Question,
                    SqlQuery = example.SqlQuery,
                    Category = example.Category,
                    EmbeddingText = embeddingText.ToString()
                };

                // 保存到向量存储
                string vectorId = $"qa_example_{example.Id}";
                await memory.SaveInformationAsync(
                    collectionName, 
                    id: vectorId, 
                    text: JsonConvert.SerializeObject(embeddingData), 
                    cancellationToken: default);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"为问答示例生成向量嵌入时出错：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除问答示例的向量嵌入
        /// </summary>
        /// <param name="example">问答示例</param>
        /// <returns>是否成功</returns>
        private async Task<bool> DeleteEmbeddingForExampleAsync(QAExample example)
        {
            try
            {
                SemanticTextMemory memory = await _semanticService.GetTextMemory();
                string collectionName = $"qa_examples_{example.ConnectionId}";
                string vectorId = $"qa_example_{example.Id}";

                await memory.RemoveAsync(collectionName, vectorId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除问答示例向量嵌入时出错：{ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 问答示例向量嵌入数据结构
    /// </summary>
    public class QAExampleEmbedding
    {
        /// <summary>
        /// 示例ID
        /// </summary>
        public string ExampleId { get; set; } = string.Empty;

        /// <summary>
        /// 用户问题
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// SQL查询
        /// </summary>
        public string SqlQuery { get; set; } = string.Empty;

        /// <summary>
        /// 分类
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 用于嵌入的文本
        /// </summary>
        public string EmbeddingText { get; set; } = string.Empty;
    }
}