namespace Ghost.Data.Entities.Mappings
{
    public class ScriptTemlateMap : BaseEntityMap<ScriptTemlate>
    {
        public ScriptTemlateMap() 
            : base("script_temlates")
        {
            Map(x => x.Code, "code").Length(4096);
            Map(x => x.Comment, "comment").Length(256);
        }
    }
}