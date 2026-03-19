namespace API.Http.Etags;

public static class ETagHelper
{
    public static string CreateWeakETag(uint rowVersion)
    {
        return $"W/\"{rowVersion}\"";
    }
    
    public static bool ShouldReturnNotModified(string? header, uint currentRowVersion)
    {
        if (string.IsNullOrWhiteSpace(header))
            return false;
        
        return MatchesAny(header, currentRowVersion);
    }

    public static uint? TryParseIfMatch(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return null;

        if (header.Contains(','))
            return null;

        header = header.Trim();
        
        if (header == "*")
            return null;
    
        if (header.StartsWith("W/", StringComparison.Ordinal))
            header = header[2..];

        header = header.Trim('"');

        if (uint.TryParse(header, out var etag))
            return etag;

        return null;
    }
    
    private static bool MatchesAny(string header, uint currentRowVersion)
    {
        foreach (var token in header.Split(','))
        {
            var t = token.Trim();
            if (t == "*")
                return true;
            
            if (t.StartsWith("W/", StringComparison.Ordinal))
                t = t[2..];
            
            t = t.Trim('"');

            if (uint.TryParse(t, out var rowVersion) && rowVersion == currentRowVersion)
                return true;
        }

        return false;
    }
}