using EmailService.Abstraction;
using EmailService.Consumers.Users;
using EmailService.Senders;
using MassTransit;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<EmailSettings>(context.Configuration.GetSection("EmailSettings"));
        services.AddTransient<IEmailSender, SmtpEmailSender>();

        services.AddMassTransit(config =>
        {
            config.AddConsumer<UserRegisteredConsumer>();

            config.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host("localhost", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("user-registered-queue", e =>
                {
                    e.Bind("users");
                    e.ConfigureConsumer<UserRegisteredConsumer>(ctx);
                });
            });
        });
    })
    .Build();

await host.RunAsync();