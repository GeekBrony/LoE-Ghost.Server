using DryIoc;
using Ghost.Data;
using Ghost.Data.Entities;
using Ghost.Data.Utilities;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ghost.Scripting
{
    public class ScriptContext
    {

    }

    public interface IScriptingManager
    {
        
    }

    internal class ScriptingManager : IScriptingManager
    {
        private IContainer m_container;
        private ScriptOptions m_options;
        private Dictionary<int, Script> m_scripts;
        private InteractiveAssemblyLoader m_loader;

        public ScriptingManager(IContainer container)
        {
            m_container = container;
            m_options = ScriptOptions.Default;
            m_loader = new InteractiveAssemblyLoader();

        }

        public void Load()
        {
            m_scripts = m_container.Resolve<IRepository<ScriptTemlate>>().GetAll()
                .ToDictionary(x => x.Id, LoadScript);
        }

        public async Task<T> ExecuteAsync<T>(int id, ScriptContext context)
        {
            Script script;
            if (m_scripts.TryGetValue(id, out script) && script is Script<T>)
            {
                var result = await ((Script<T>)script).RunAsync(context, Catch);
                return result.ReturnValue;
            }
            return default(T);
        }

        private Script LoadScript(ScriptTemlate template)
        {
            switch (template.Type)
            {
                case ScriptType.None:
                    return CSharpScript.Create(template.Code, m_options, typeof(ScriptContext), m_loader);
                case ScriptType.Condition:
                    return CSharpScript.Create<bool>(template.Code, m_options, typeof(ScriptContext), m_loader);
                default: throw new NotImplementedException();
            }
        }

        private bool Catch(Exception eception)
        {
            return false;
        }
    }
}