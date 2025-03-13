﻿using System.Text.Json;

namespace Text2Sql.Net.Utils
{
    internal static class ObjectExtensions
    {
        private static readonly JsonSerializerOptions s_jsonOptionsCache = new() { WriteIndented = true };

        public static string AsJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, s_jsonOptionsCache);
        }
    }
}
