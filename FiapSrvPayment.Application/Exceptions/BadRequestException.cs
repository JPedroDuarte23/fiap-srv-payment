using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiapSrvPayment.Application.Exceptions;

[ExcludeFromCodeCoverage]
public class BadRequestException : HttpException
{
    public BadRequestException(string message)
        : base(400, "Bad Request", message) { }
}