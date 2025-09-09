using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.Domain.Entities;
public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = "";   // e.g. MoodEntry.Created
    public string Level { get; set; } = "Info"; // Info|Warn|Error
    public string Data { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

