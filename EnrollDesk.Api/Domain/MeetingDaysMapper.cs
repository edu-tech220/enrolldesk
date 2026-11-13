namespace EnrollDesk.Api.Domain;

/// <summary>Converts between the <see cref="MeetingDays"/> flags enum and plain day names for JSON payloads.</summary>
public static class MeetingDaysMapper
{
    private static readonly (MeetingDays Flag, string Name)[] Map =
    {
        (MeetingDays.Monday, "Monday"),
        (MeetingDays.Tuesday, "Tuesday"),
        (MeetingDays.Wednesday, "Wednesday"),
        (MeetingDays.Thursday, "Thursday"),
        (MeetingDays.Friday, "Friday"),
        (MeetingDays.Saturday, "Saturday"),
        (MeetingDays.Sunday, "Sunday")
    };

    public static List<string> ToNames(MeetingDays days) =>
        Map.Where(m => days.HasFlag(m.Flag)).Select(m => m.Name).ToList();

    public static MeetingDays Parse(IEnumerable<string> names)
    {
        var result = MeetingDays.None;
        foreach (var name in names)
        {
            var match = Map.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match.Name is null)
            {
                throw new ArgumentException($"'{name}' is not a valid day of the week.");
            }
            result |= match.Flag;
        }
        return result;
    }
}
