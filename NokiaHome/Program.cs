using NokiaHome.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpClient and Entur services
builder.Services.AddHttpClient<NokiaHome.Services.IEnturGraphQLService, NokiaHome.Services.EnturGraphQLService>();
builder.Services.AddHttpClient<NokiaHome.Services.IEnturGeocodingService, NokiaHome.Services.EnturGeocodingService>();

// Linear integration — API key supplied via Linear__ApiKey environment variable
builder.Services.Configure<LinearSettings>(builder.Configuration.GetSection("Linear"));
builder.Services.AddHttpClient<NokiaHome.Services.ILinearService, NokiaHome.Services.LinearService>();

// Blob storage — connection string supplied via BlobStorage__ConnectionString environment variable
builder.Services.Configure<NokiaHome.Settings.BlobStorageSettings>(builder.Configuration.GetSection("BlobStorage"));
builder.Services.AddScoped<NokiaHome.Services.IBlobStorageService, NokiaHome.Services.BlobStorageService>();

// Calendar
builder.Services.AddScoped<NokiaHome.Services.ICalendarService, NokiaHome.Services.CalendarService>();

// PDF processing
builder.Services.AddScoped<NokiaHome.Services.IPdfImageExtractionService, NokiaHome.Services.PdfImageExtractionService>();
builder.Services.AddScoped<NokiaHome.Services.IPdfTextExtractionService, NokiaHome.Services.PdfTextExtractionService>();

// Voice-to-calendar — OpenAI Whisper + GPT. API key supplied via OpenAi__ApiKey environment variable
builder.Services.Configure<OpenAiSettings>(builder.Configuration.GetSection("OpenAi"));
builder.Services.AddHttpClient<NokiaHome.Services.IVoiceEventService, NokiaHome.Services.VoiceEventService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
