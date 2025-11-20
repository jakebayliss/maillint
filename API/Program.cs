var builder = WebApplication.CreateBuilder(args);

// CORS for local development (Outlook Add-in pages hosted on localhost)
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
		policy
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials()
			.SetIsOriginAllowed(origin =>
			{
				try
				{
					var host = new Uri(origin).Host;
					return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
					       string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
				}
				catch
				{
					return false;
				}
			}));
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// LLM HTTP client (currently configured for Anthropic-compatible endpoints)
builder.Services.AddHttpClient<API.Services.LLMClient>(client =>
{
	var endpoint = builder.Configuration["LLM:Endpoint"];
	var key = builder.Configuration["LLM:Key"];
	var timeoutSeconds = builder.Configuration.GetValue<int?>("LLM:TimeoutSeconds") ?? 180;
	if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
	{
		throw new InvalidOperationException("LLM configuration missing. Set LLM:Endpoint and LLM:Key.");
	}

	client.BaseAddress = new Uri(endpoint.TrimEnd('/') + "/v1/");
	// Prefer standard Bearer auth; some providers also accept x-api-key
	client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
	// Moonshot Anthropic-compatible API requires anthropic-version
	client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
	client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
	client.DefaultRequestHeaders.UserAgent.ParseAdd("OutlookAI/1.0");
	client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.MapControllers();

app.Run();
