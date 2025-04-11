using EmailService.Abstraction;
using EmailService.Consumers.Auth;
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
            config.AddConsumer<VerificationCodeResentConsumer>();

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("rabbitmq", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("email-service-user-registered-queue", e =>
                {
                    e.Bind("user-registered", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<UserRegisteredConsumer>(context);
                });
                
                
                cfg.ReceiveEndpoint("email-service-verification-code-resent-queue", e =>
                {
                    e.Bind("verification-code-resent", x =>
                    {
                        x.ExchangeType = "fanout";
                    });

                    e.ConfigureConsumer<VerificationCodeResentConsumer>(context);
                });
            });
        });
    })
    .Build();

await host.RunAsync();