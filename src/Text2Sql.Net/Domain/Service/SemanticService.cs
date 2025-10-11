using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Postgres;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using Microsoft.SemanticKernel.Memory;
using Npgsql;
using Newtonsoft.Json;
using Polly;
using Text2Sql.Net;
using Text2Sql.Net.Domain.Interface;
using Text2Sql.Net.Options;
using Text2Sql.Net.Utils;


namespace Text2Sql.Net.Domain.Service
{
    /// <summary>
    /// 语义服务实现
    /// </summary>
    [ServiceDescription(typeof(ISemanticService), ServiceLifetime.Scoped)]
    public class SemanticService(Kernel _kernel) : ISemanticService
    {
    
        /// <summary>
        /// 获取SemanticTextMemory
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<SemanticTextMemory> GetTextMemory()
        {
            IMemoryStore memoryStore = null;
            switch (Text2SqlConnectionOption.DbType)
            {
                case "Sqlite":
                    memoryStore = await SqliteMemoryStore.ConnectAsync(Text2SqlConnectionOption.VectorConnection);
                    break;
                case "PostgreSQL":
                    NpgsqlDataSourceBuilder dataSourceBuilder = new(Text2SqlConnectionOption.VectorConnection);
                    dataSourceBuilder.UseVector();
                    NpgsqlDataSource dataSource = dataSourceBuilder.Build();
                    memoryStore = new PostgresMemoryStore(dataSource, vectorSize: 1536, schema: "public");
                    break;
            }
            if (memoryStore == null)
            {
                throw new InvalidOperationException("GraphDBConnection error failed to initialize memory store.");
            }

            var handler = new OpenAIHttpClientHandler(isEmbeddingHandler: true);
            var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            var embeddingGenerator = new OpenAITextEmbeddingGenerationService(Text2SqlOpenAIOption.EmbeddingModel, Text2SqlOpenAIOption.GetEmbeddingKey, httpClient: httpClient);
            SemanticTextMemory textMemory = new(memoryStore, embeddingGenerator);

            return textMemory;
        }
    }
}
