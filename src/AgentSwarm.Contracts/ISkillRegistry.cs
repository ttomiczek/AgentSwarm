namespace AgentSwarm.Contracts;

public interface ISkillRegistry
{
    IReadOnlyList<SkillEntry> GetAllSkills();
    SkillEntry? GetSkill(string name);
}

public record SkillEntry(
    string Name,
    string Description,
    IReadOnlyList<ToolSchema>? Tools,
    string Markdown
);
