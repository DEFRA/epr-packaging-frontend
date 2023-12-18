namespace FrontendSchemeRegistration.Application.Services;

using Interfaces;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class PatchService : IPatchService
{
    private const char PathDelimiter = '/';
    private const char Dash = '-';

    public JsonPatchDocument CreatePatchDocument<T>(T originalObject, T modifiedObject)
        where T : class
    {
        var original = JObject.FromObject(originalObject);
        var modified = JObject.FromObject(modifiedObject);
        var patch = new JsonPatchDocument();
        FillPatchForObject(original, modified, patch, "/");
        return patch;
    }

    private static void FillPatchForObject(JObject orig, JObject mod, JsonPatchDocument patch, string path)
    {
        var origNames = orig.Properties().Select(x => x.Name).ToArray();
        var modNames = mod.Properties().Select(x => x.Name).ToArray();

        PatchRemovedProperty(patch, origNames, modNames, path);
        PatchAddedProperty(patch, origNames, modNames, path, mod);

        foreach (var k in origNames.Intersect(modNames))
        {
            var origProp = orig.Property(k);
            var modProp = mod.Property(k);

            if (origProp.Value.Type != modProp.Value.Type)
            {
                patch.Replace(path + modProp.Name, modProp.Value.ToObject<object>());
            }
            else if (!string.Equals(
                         origProp.Value.ToString(Formatting.None),
                         modProp.Value.ToString(Formatting.None)))
            {
                PatchPropertyValueChange(patch, origProp, modProp, path);
            }
        }
    }

    private static void PatchPropertyValueChange(JsonPatchDocument patch, JProperty origProp, JProperty modProp, string path)
    {
        if (origProp.Value.Type == JTokenType.Object)
        {
            FillPatchForObject(origProp.Value as JObject, modProp.Value as JObject, patch, path + modProp.Name + PathDelimiter);
        }
        else
        {
            if (origProp.Value.Type == JTokenType.Array)
            {
                var oldArray = origProp.Value as JArray;
                var newArray = modProp.Value as JArray;
                PatchArray(patch, oldArray, newArray, path + modProp.Name);
            }
            else
            {
                patch.Replace(path + modProp.Name, modProp.Value.ToObject<object>());
            }
        }
    }

    private static void PatchArray(JsonPatchDocument patch, JArray oldArray, JArray newArray, string path)
    {
        for (int i = 0; i < oldArray.Count; i++)
        {
            if (newArray.Count - 1 < i)
            {
                patch.Remove(path + PathDelimiter + i);
            }
            else
            {
                if (oldArray[i] is JObject && newArray[i] is JObject)
                {
                    FillPatchForObject(oldArray[i] as JObject, newArray[i] as JObject, patch, path + PathDelimiter + i + PathDelimiter);
                }
                else
                {
                    if (!JToken.DeepEquals(oldArray[i], newArray[i]))
                    {
                        patch.Replace(path + PathDelimiter + i, newArray[i]);
                    }
                }
            }
        }

        for (int i = oldArray.Count; i < newArray.Count; i++)
        {
            patch.Add(path + PathDelimiter + Dash, newArray[i]);
        }
    }

    private static void PatchAddedProperty(JsonPatchDocument patch, string[] origNames, string[] modNames, string path, JObject mod)
    {
        foreach (var k in modNames.Except(origNames))
        {
            var prop = mod.Property(k);
            patch.Add(path + prop.Name, prop.Value.ToObject<object>());
        }
    }

    private static void PatchRemovedProperty(JsonPatchDocument patch, string[] origNames, string[] modNames, string path)
    {
        foreach (var k in origNames.Except(modNames))
        {
            patch.Remove(path + k);
        }
    }
}