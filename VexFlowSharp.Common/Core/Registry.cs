#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    public class RegistryUpdate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? OldValue { get; set; }
    }

    /// <summary>
    /// Indexes VexFlow elements by id, type, and class. Port of VexFlow's Registry from registry.ts.
    /// </summary>
    public class Registry
    {
        private static Registry? defaultRegistry;

        private readonly Dictionary<string, Dictionary<string, Dictionary<string, Element>>> index =
            new Dictionary<string, Dictionary<string, Dictionary<string, Element>>>
        {
            { "id", new Dictionary<string, Dictionary<string, Element>>() },
            { "type", new Dictionary<string, Dictionary<string, Element>>() },
            { "class", new Dictionary<string, Dictionary<string, Element>>() },
        };

        public static Registry? GetDefaultRegistry() => defaultRegistry;

        public static void EnableDefaultRegistry(Registry registry)
        {
            defaultRegistry = registry;
        }

        public static void DisableDefaultRegistry()
        {
            defaultRegistry = null;
        }

        public Registry Clear()
        {
            foreach (var attribute in index.Values)
                attribute.Clear();
            return this;
        }

        public void SetIndexValue(string name, string value, string id, Element elem)
        {
            if (!index.TryGetValue(name, out var byValue))
            {
                byValue = new Dictionary<string, Dictionary<string, Element>>();
                index[name] = byValue;
            }

            if (!byValue.TryGetValue(value, out var byId))
            {
                byId = new Dictionary<string, Element>();
                byValue[value] = byId;
            }

            byId[id] = elem;
        }

        public void UpdateIndex(RegistryUpdate update)
        {
            var elem = GetElementById(update.Id);
            if (update.OldValue != null
                && index.TryGetValue(update.Name, out var byOldValue)
                && byOldValue.TryGetValue(update.OldValue, out var oldById))
            {
                oldById.Remove(update.Id);
            }

            if (!string.IsNullOrEmpty(update.Value) && elem != null)
            {
                var currentId = elem.GetAttribute("id") ?? update.Id;
                SetIndexValue(update.Name, update.Value!, currentId, elem);
            }
        }

        public Registry Register(Element elem, string? id = null)
        {
            id ??= elem.GetAttribute("id");
            if (string.IsNullOrEmpty(id))
                throw new VexFlowException("BadArguments", "Can't add element without `id` attribute to registry");

            elem.SetAttribute("id", id);
            SetIndexValue("id", id, id, elem);

            var type = elem.GetAttribute("type");
            if (!string.IsNullOrEmpty(type))
                SetIndexValue("type", type!, id, elem);

            foreach (var className in elem.GetClasses())
                SetIndexValue("class", className, id, elem);

            elem.OnRegister(this);
            return this;
        }

        public Element? GetElementById(string id)
        {
            return index.TryGetValue("id", out var byValue)
                && byValue.TryGetValue(id, out var byId)
                && byId.TryGetValue(id, out var elem)
                ? elem
                : null;
        }

        public List<Element> GetElementsByAttribute(string attribute, string value)
        {
            var result = new List<Element>();
            if (!index.TryGetValue(attribute, out var byValue)
                || !byValue.TryGetValue(value, out var byId))
                return result;

            result.AddRange(byId.Values);
            return result;
        }

        public List<Element> GetElementsByType(string type) => GetElementsByAttribute("type", type);

        public List<Element> GetElementsByClass(string className) => GetElementsByAttribute("class", className);

        public Registry OnUpdate(RegistryUpdate update)
        {
            if (update.Name == "id" || update.Name == "type" || update.Name == "class")
                UpdateIndex(update);
            return this;
        }
    }
}
