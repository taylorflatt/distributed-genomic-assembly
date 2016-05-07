using Genome.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Genome.Helpers
{
    public class HelperMethods
    {
        protected internal static List<string> ParseUrlString(string urlString)
        {
            return urlString.Split(',').Select(sValue => sValue.Trim()).ToList();
        }
    }
}