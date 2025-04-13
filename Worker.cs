using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using System.Runtime;
using System.Text;

namespace ReportJobRunner
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string connectionString = string.Empty;
        private readonly string _secretApiKey = string.Empty;
        private readonly string _apiPrefix = string.Empty;
        private static readonly HttpClient client = new();
        private readonly AppSettings _settings;

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            _settings = options.Value;
            _secretApiKey = $"{_settings.SecretKey}" ?? string.Empty;
            connectionString = $"{_settings.ConnectionString}" ?? string.Empty;
            _apiPrefix = $"{_settings.APIPrefix}" ?? string.Empty;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    string query = "EXEC GET_LOOK_FOR_FINAL_REPORT_REQUEST;";
                    DataTable reportListTable = new DataTable();
                    using SqlConnection connection = new(connectionString);
                    using SqlCommand command = new(query, connection);
                    try
                    {
                        connection.Open();

                        using SqlDataReader reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reportListTable.Load(reader);
                            reader?.DisposeAsync();
                            List<int> finalReportIds = [.. reportListTable.AsEnumerable().Select(row => Convert.ToInt32(row[0]))];
                            foreach (int ReportId in finalReportIds)
                            {
                                string API = _apiPrefix + "/api/Finance/ProcessNewFinalReportRequest";
                                // Add Account Number to Header
                                client.DefaultRequestHeaders.Clear();
                                client.DefaultRequestHeaders.Add("X-API-KEY", _secretApiKey);
                                var content = new StringContent(ReportId.ToString(), Encoding.UTF8, "application/json");

                                HttpResponseMessage response = await client.PutAsync(API, content);

                                response.EnsureSuccessStatusCode(); // Throws if not successful

                                string responseData = await response.Content.ReadAsStringAsync();
                            }
                        }
                        else
                        {
                            await Task.Delay(60000 * 3, stoppingToken);
                        }
                    }
                    catch (SqlException ex)
                    {
                        _logger.LogError(
                                         string.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                     );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                                       string.Format("An unexpected error occurred while executing the query.\n Error Details:\n{0}", ex.Message)
                                   );
                    }
                    finally
                    {
                        reportListTable?.Dispose();
                    }
                }
            }
        }
    }
}
