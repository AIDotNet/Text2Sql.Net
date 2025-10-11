namespace Text2Sql.Net.Options
{
    public class Text2SqlOpenAIOption
    {
        /// <summary>
        /// 聊天模型的端点地址
        /// </summary>
        public static string? EndPoint { get; set; }

        /// <summary>
        /// 聊天模型的API密钥
        /// </summary>
        public static string? Key { get; set; }

        /// <summary>
        /// 向量模型的端点地址（如果为空则使用聊天模型的EndPoint）
        /// </summary>
        public static string? EmbeddingEndPoint { get; set; }

        /// <summary>
        /// 向量模型的API密钥（如果为空则使用聊天模型的Key）
        /// </summary>
        public static string? EmbeddingKey { get; set; }

        /// <summary>
        /// 聊天模型名称
        /// </summary>
        public static string? ChatModel { get; set; }

        /// <summary>
        /// 向量模型名称
        /// </summary>
        public static string? EmbeddingModel { get; set; }

        /// <summary>
        /// 获取向量模型的端点地址（如果EmbeddingEndPoint为空则返回EndPoint）
        /// </summary>
        public static string? GetEmbeddingEndPoint => string.IsNullOrWhiteSpace(EmbeddingEndPoint) ? EndPoint : EmbeddingEndPoint;

        /// <summary>
        /// 获取向量模型的API密钥（如果EmbeddingKey为空则返回Key）
        /// </summary>
        public static string? GetEmbeddingKey => string.IsNullOrWhiteSpace(EmbeddingKey) ? Key : EmbeddingKey;
    }
}
