using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBuildingBlocks.Domain.Models
{
    public class AuditLog 
    {
        public string TableName { get; set; } = null!;
        public string Action { get; set; } = null!; // Insert / Update / Delete
        public string KeyValues { get; set; } = null!;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string PerformedBy { get; set; } = "System";
        public string IPAddress { get; set; } = "";
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }
}
