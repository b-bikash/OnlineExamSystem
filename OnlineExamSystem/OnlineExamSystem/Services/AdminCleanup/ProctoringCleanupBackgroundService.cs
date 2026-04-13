using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineExamSystem.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineExamSystem.Services.AdminCleanup
{
    public class ProctoringCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<ProctoringCleanupBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ProctoringCleanupBackgroundService(
            ILogger<ProctoringCleanupBackgroundService> logger, 
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Proctoring Cleanup Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldImagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Proctoring Cleanup.");
                }

                // Wait 24 hours before checking again
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CleanupOldImagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            // Target images older than 7 days
            var thresholdDate = DateTime.UtcNow.AddDays(-7);

            var expiredLogs = await context.ExamProctorLogs
                .Where(l => l.CapturedAt < thresholdDate && l.ImagePath != "EXPIRED")
                .ToListAsync(stoppingToken);

            if (!expiredLogs.Any())
            {
                return;
            }

            int deletedFiles = 0;

            foreach (var log in expiredLogs)
            {
                if (!string.IsNullOrEmpty(log.ImagePath) && log.ImagePath != "EXPIRED")
                {
                    var fileName = Path.GetFileName(log.ImagePath);
                    var physicalPath = Path.Combine(env.WebRootPath, "proctoring", fileName);

                    if (File.Exists(physicalPath))
                    {
                        try
                        {
                            File.Delete(physicalPath);
                            deletedFiles++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Could not delete physical image file: {physicalPath}");
                        }
                    }
                }

                // Preserve DB record but mark image as deleted
                log.ImagePath = "EXPIRED";
            }

            await context.SaveChangesAsync(stoppingToken);
            _logger.LogInformation($"Proctoring Cleanup Complete: Deleted {deletedFiles} physical images and updated {expiredLogs.Count} database records.");
        }
    }
}
