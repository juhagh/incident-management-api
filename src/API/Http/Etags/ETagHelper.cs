namespace API.Http.Etags;

public static class ETagHelper
{
    public static string CreateWeakETag(uint rowVersion)
    {
        return $"W/\"{rowVersion}\"";
    }
    
    public static bool ShouldReturnNotModified(string? ifNoneMatchHeader, uint currentRowVersion)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatchHeader))
            return false;
        
        // ETag: Match Strong and Weak
        foreach (var token in ifNoneMatchHeader.Split(','))
        {
            var t = token.Trim();
            if (t == "*")
                return true;
            
            if (t.StartsWith("W/", StringComparison.Ordinal))
                t = t.Substring(2);
            
            t = t.Trim('"');

            if (uint.TryParse(t, out var rowVersion) && rowVersion == currentRowVersion)
                return true;
        }
        return false;
    }
}