using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Markdig;

namespace Moonglade.Notification.Core
{
    public class Utils
    {
        public static string AppVersion =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

        public static string MdContentToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();
            var result = Markdown.ToHtml(markdown, pipeline);
            return result;
        }
    }
}
