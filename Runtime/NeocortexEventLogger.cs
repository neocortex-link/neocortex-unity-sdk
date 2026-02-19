using System;
using System.Linq;
using UnityEngine;
using Neocortex.Data;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace Neocortex
{
    public static class NeocortexEventLogger
    {
        private const int MaxTotal = 20;
        private const int MaxContentLength = 64;
        
        private static readonly List<EventLog> buffer = new();
        
        public static void Push(EventLog log)
        {
            if (log.content != null && log.content.Length > MaxContentLength)
            {
                Debug.LogWarning($"NeocortexEventLog dropped (content > {MaxContentLength} chars): {log.content}");
                return;
            }

            buffer.Add(log);
        }

        public static void Push(EventPriority priority, string content, string date = "")
        {
            Push(new EventLog(priority, content, date));
        }
        
        public static string GetLogs()
        {
            if (buffer.Count == 0) return "";

            var selectedIndexes = new HashSet<int>();
            var priorities = Enum.GetValues(typeof(EventPriority))
                .Cast<EventPriority>()
                .OrderByDescending(p => p);

            int count = 0;

            // Priority-first, newest-first selection
            foreach (var priority in priorities)
            {
                for (int i = buffer.Count - 1; i >= 0; i--)
                {
                    if (buffer[i].priority == priority)
                    {
                        selectedIndexes.Add(i);
                        count++;

                        if (count == MaxTotal)
                            break;
                    }
                }

                if (count == MaxTotal)
                    break;
            }

            // Emit in original order
            var result = new List<EventLog>(selectedIndexes.Count);
            for (int i = 0; i < buffer.Count; i++)
            {
                if (selectedIndexes.Contains(i))
                    result.Add(buffer[i]);
            }
            
            return JsonConvert.SerializeObject(result);
        }
        
        public static void Clear()
        {
            buffer.Clear();
        }
    }
}