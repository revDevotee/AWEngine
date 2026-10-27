using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace AWEngine
{
    public abstract class EcsSystem
    {
        public void UpdateCore(IReadOnlyList<EcsEntity> entities)
        {
            try
            {
                Update(entities);
            }
            catch (Exception ex)
            {
                Console.Clear();
                string error = $"""
                    An error occurred while executing the code:
                    -{ex.Source}
                    -{ex.Message}
                    -{ex.GetType().FullName}
                    
                    {ex.StackTrace}
                    
                    Press any button to continue...
                    """;
                Console.WriteLine();
                Console.ReadKey();
                return;
            }
        }
        public abstract void Update(IReadOnlyList<EcsEntity> entities);
    }
    public abstract class EcsComponent
    {
        public EcsEntity EcsEntity { get; set; }
    }


    public class EcsEntity
    {
        private static int _nextId = 0;
        private int Id { get; }
        private readonly List<EcsComponent> _components = new List<EcsComponent>();
        //???? revork sometime
        public EcsEntity()
        {
            Id = _nextId++;
        }
        /// <summary>
        /// Adding Component. Exactly
        /// </summary>
        /// <param name="component">Component to add</param>
        public EcsEntity AddComponent(EcsComponent component)
        {
            component.EcsEntity = this;
            _components.Add(component);
            return this;
        }

        /// <summary>
        /// Remove Component from entity
        /// </summary>
        /// <typeparam name="T">Component to remove</typeparam>
        public void RemoveComponent<T>() where T : EcsComponent
        {
            foreach (var component in _components)
            {
                if (component is T)
                {
                    _components.Remove((T)component);
                    break;
                }
            }
        }
        /// <summary>
        /// Swap T Component on S Component
        /// </summary>
        /// <typeparam name="T">Which component to swap</typeparam>
        /// <typeparam name="S">Component to which swap</typeparam>
        public void SwapComponent<T>(EcsComponent S) where T : EcsComponent
        {
            var componentToRemove = _components.FirstOrDefault(c => c is T);
            if (componentToRemove != null) _components.Remove(componentToRemove);
            S.EcsEntity = this;
            _components.Add(S);
        }
        /// <summary>
        /// Get component. Literraly
        /// </summary>
        /// <typeparam name="T">Component to get</typeparam>
        /// <returns></returns>
        public T GetComponent<T>() where T : EcsComponent
        {
            foreach (var component in _components)
            {
                if (component is T) return (T)component;
            }
            return null;
        }

        /// <summary>
        /// Chekning, does entity has a Component?
        /// </summary>
        /// <typeparam name="T">Component to find</typeparam>
        /// <returns></returns>
        public bool HasComponent<T>() where T : EcsComponent
        {
            return GetComponent<T>() != null;
        }
    }

    public class WorldContext
    {
        private readonly List<EcsEntity> _entities = new List<EcsEntity>();
        public IReadOnlyList<EcsEntity> Entities => _entities;
        public void AddEntity(EcsEntity entity)
        {
            if (entity == null) throw new ArgumentException("Entity can't be empty!");

            else if (_entities.Contains(entity)) throw new InvalidOperationException("This entity already exists!");
            _entities.Add(entity);
        }
        public void Cleanup<T>() where T : EcsComponent
        {
            _entities.RemoveAll(e => e.HasComponent<T>());
        }
    }
    public class SystemManager
    {
        // linq too hard
        /// <summary>
        /// Stores ECS systems grouped by execution phase type.
        /// </summary>
        private readonly Dictionary<ExecutionPhase, List<EcsSystem>> _systems = new Dictionary<ExecutionPhase, List<EcsSystem>>();

        /// <summary>
        /// Lists of systems indexed by execution phase.
        /// </summary>
        /// <returns>Array of read-only system lists grouped by type.</returns>
        public IReadOnlyDictionary<ExecutionPhase, List<EcsSystem>> Systems => _systems;

        /// <summary>
        /// Adding a single system to a specific execution phase.
        /// </summary>
        /// <param name="type">Execution phase type.</param>
        /// <param name="system">System to add.</param>
        /// <returns></returns>
        public SystemManager Add(ExecutionPhase phase, EcsSystem system)
        {
            if (!_systems.ContainsKey(phase))
            {
                _systems.Add(phase, new List<EcsSystem>());
            }
            _systems[phase].Add(system);
            return this;
        }

        /// <summary>
        /// Adding multiple systems to a specific execution phase.
        /// </summary>
        /// <param name="type">Execution phase type.</param>
        /// <param name="system">List of systems to add.</param>
        /// <returns></returns>
        public SystemManager Add(ExecutionPhase phase, List<EcsSystem> system)
        {
            if (!_systems.ContainsKey(phase))
            {
                _systems.Add(phase, new List<EcsSystem>());
            }
            _systems[phase].AddRange(system);
            return this;
        }

        /// <summary>
        /// Remove a system from a specific execution phase.
        /// </summary>
        /// <param name="type">Execution phase type.</param>
        /// <param name="system">System to remove.</param>
        /// <returns></returns>
        public SystemManager Remove(ExecutionPhase phase, EcsSystem system)
        {
            _systems[phase].Remove(system);
            return this;
        }

        /// <summary>
        /// Executes all registered systems for the specified phase.
        /// </summary>
        /// <param name="wc">World context with entities</param>
        /// <param name="type">Execution phase type to run.</param>
        /// <returns></returns>
        public SystemManager Update(WorldContext wc, ExecutionPhase phase)
        {
            foreach (EcsSystem sys in _systems[phase])
            {
                sys.UpdateCore(wc.Entities);
            }
            return this;
        }
    }
    /// <summary>
    /// Defines a custom execution phase used to organize ECS systems.
    /// You can create your own phases, such as Simulation, Actions, or Rendering, 
    /// and use them with SystemManager to register and run systems in the appropriate order.
    /// Example: public static class GameSystems { public static ExecutionPhase Render = new ExecutionPhase(); } 
    /// </summary>
    public class ExecutionPhase
    {

    }
    /// <summary>
    /// placeholder for modding in future :D
    /// </summary>
    public class AWEngineAPI
    {
        private WorldContext _context = new WorldContext();
        private SystemManager _systemManager = new SystemManager();
    }
    //placeholder
    interface IMod
    {
        public abstract void Mod();
    }
    public abstract class AWEngine
    {
        public bool running = true;
        protected abstract void Init();
        protected abstract void Update();
        public void Run(bool FastSplash = false)
        {
            try
            {
                SplashScreen(FastSplash);
                Init();
                while (running)
                {
                    Update();
                }
            }
            catch (Exception ex)
            {
                string FUCK = $"""
                    ======================================
                          CRITICAL ERROR OCCURRED :(
                    ======================================
                    [SOURCE] - {ex.Source}
                    [TYPE] - {ex.GetType().FullName}
                    [MESSAGE] - {ex.Message}
                    
                    [STACKTRACE]:
                    {ex.StackTrace}
                    
                    Press any button to continue...
                    """;
                Console.Clear();
                Console.WriteLine(FUCK);
                Console.ReadKey();
            }
        }
        /// <summary>
        /// Really cool splash screen about engine. Pls use it :D
        /// </summary>
        /// <param name="FastSplash">if true, it will show just text for 1.5 sec</param>
        public void SplashScreen(bool FastSplash)
        {
            if (!FastSplash)
            {
                string splash = """
                    __________       ___________              _____             
                    ___    |_ |     / /__  ____/_____________ ___(_)___________ 
                    __  /| |_ | /| / /__  __/  __  __ \_  __ `/_  /__  __ \  _ \
                    _  ___ |_ |/ |/ / _  /___  _  / / /  /_/ /_  / _  / / /  __/
                    /_/  |_|___/|__/  /_____/  /_/ /_/_\__, / /_/  /_/ /_/\___/ 
                                                      /____/                    
                    """;
                Console.Write($"                       Powered with\n{splash}");
                Console.Write($"\n\n                  AWEngine by revDevotee");
                Console.Write($"\n               All rights are NOT reserved");
                Console.Write($"\n\n                     Initializing [ ]");
                Console.SetCursorPosition(Console.GetCursorPosition().Left - 1, Console.GetCursorPosition().Top);
                string[] chars = new string[] { "-", @"\", @"|", @"/"}; 
                for (int i = 0; i < 6; i++)
                {
                    foreach(string _ in chars)
                    {
                        Console.SetCursorPosition(Console.GetCursorPosition().Left - 1, Console.GetCursorPosition().Top);
                        Console.Write(_);
                        Thread.Sleep(150);
                    }
                }
            }
            else
            {
                Console.SetCursorPosition(0, 2);
                Console.WriteLine("Powered with AWEngine by revDevotee. All right are NOT reserved.");
                Thread.Sleep(1500);
            }
        }
    }
}
