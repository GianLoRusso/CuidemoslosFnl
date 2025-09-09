using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuidemoslos.Services.Email;
public interface IEmailSender { Task SendAsync(string to, string subject, string html); }
