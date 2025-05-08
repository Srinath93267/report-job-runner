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
        private readonly ApiSettings _settings;

        public Worker(ILogger<Worker> logger, IOptions<ApiSettings> options)
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
                    if (!String.IsNullOrEmpty(connectionString))
                    {
                        string query = "EXEC GET_LOOK_FOR_FINAL_REPORT_REQUEST;";
                        DataTable reportListTable = new();
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
                                    await Task.Delay(2000, stoppingToken);//2 Seconds of gap for processing each report
                                    Console.WriteLine($"Processing the Report ID: {ReportId}");

                                    string API = _apiPrefix + "/api/Finance/ProcessNewFinalReportRequest";//API Endpoint preparation

                                    client.DefaultRequestHeaders.Clear();
                                    client.DefaultRequestHeaders.Add("X-API-KEY", _secretApiKey);//Add the secret-key in the header of the request

                                    var content = new StringContent(ReportId.ToString(), Encoding.UTF8, "application/json");

                                    HttpResponseMessage response = await client.PutAsync(API, content, stoppingToken);

                                    response.EnsureSuccessStatusCode(); // Throws if not successful

                                    string responseData = await response.Content.ReadAsStringAsync(stoppingToken);

                                    Console.WriteLine($"Report ID: {ReportId} has been processed successfully.");
                                }
                            }
                            else
                            {
                                //Sleep for next 10 Seconds if there are no pending requests
                                Console.WriteLine("There are no pending Final Requests at the Moment. Sleeping for 10 Seconds.");
                                await Task.Delay(10000, stoppingToken);
                            }
                        }
                        catch (SqlException ex)
                        {
                            _logger.LogError($"An unexpected error occurred while executing the query.\nError Details:\n{ex}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"An unexpected error occurred while processing the requests.\nError Details:\n{ex}");
                        }
                        finally
                        {
                            //Dispose the data table and connections if it is not null
                            reportListTable?.Dispose();
                            command?.Dispose();
                            connection?.Dispose();
                        }
                    }
                    else
                    {
                        //Sleep for 12 hours if the connection string is null
                        Console.WriteLine($"Connection String is Empty or Null \n Connection String: {connectionString}");
                        Console.WriteLine("Going to Sleep for next 12 hours");
                        await Task.Delay(60000 * 60 * 12, stoppingToken);
                    }
                }
            }
        }
    }
}
