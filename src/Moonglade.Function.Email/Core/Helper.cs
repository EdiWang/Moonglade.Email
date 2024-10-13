﻿using Edi.TemplateEmail;

namespace Moonglade.Function.Email.Core;

public class Helper
{
    public static EmailHelper GetEmailHelper(string functionAppDirectory)
    {
        var configSource = Path.Join(functionAppDirectory, "mailConfiguration.xml");
        if (!File.Exists(configSource))
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);

        var smtpSettings = new SmtpSettings(EnvHelper.Get<string>("SmtpServer"),
            EnvHelper.Get<string>("SmtpUserName"),
            EnvHelper.Get<string>("EmailAccountPassword", EnvironmentVariableTarget.Process),
            EnvHelper.Get<int>("SmtpServerPort"));

        if (EnvHelper.Get<bool>("EnableTls"))
        {
            smtpSettings.EnableTls = true;
        }

        var settings = new EmailSettings
        {
            SmtpSettings = smtpSettings
        };

        var dName = EnvHelper.Get<string>("SenderDisplayName");
        if (!string.IsNullOrWhiteSpace(dName)) settings.EmailDisplayName = dName;

        var emailHelper = new EmailHelper(configSource, settings);
        return emailHelper;
    }
}