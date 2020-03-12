using System.Collections.Generic;

namespace Moonglade.Notification.Models
{
    public class ApiKey
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Key { get; set; }
        public IReadOnlyCollection<string> Roles { get; set; }

        public ApiKey()
        {
            Roles = new[] { "Administrator" };
        }
    }
}
