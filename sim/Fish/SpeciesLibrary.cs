namespace TheLedger.Sim.Fish;

/// <summary>Loads species files with their body shapes. The caller supplies file access (Godot res:// or disk).</summary>
public static class SpeciesLibrary
{
    /// <param name="speciesJson">Each species file's text, in a fixed order (file name order).</param>
    /// <param name="readShape">Returns a shape file's text by its ref (e.g. "shapes/centrarchid_bass.json"), or null.</param>
    /// <param name="errors">Problems found (missing shapes fall back to the default outline).</param>
    public static List<SpeciesSpec> Load(IEnumerable<string> speciesJson, Func<string, string?> readShape, List<string> errors)
    {
        var list = new List<SpeciesSpec>();
        var shapes = new Dictionary<string, ShapeSpec>();
        foreach (string json in speciesJson)
        {
            SpeciesSpec sp;
            try { sp = SpeciesSpec.Load(json); }
            catch (Exception ex) { errors.Add($"species: {ex.Message}"); continue; }
            string r = sp.Body.ShapeRef;
            if (!shapes.TryGetValue(r, out var shape))
            {
                string? text = string.IsNullOrEmpty(r) ? null : readShape(r);
                if (text == null) { errors.Add($"{sp.Id}: no shape '{r}' (using the default outline)"); shape = new ShapeSpec(); }
                else shape = ShapeSpec.Load(text);
                shapes[r] = shape;
            }
            sp.Shape = shape;
            list.Add(sp);
        }
        return list;
    }
}
