#region License
//  Copyright 2015 John K�ll�n
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
#endregion

using Pytocs.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace Pytocs.TypeInference
{
    /// <summary>
    /// Provides a factory for python source ASTs.  Maintains configurable on-disk and
    /// in-memory caches to avoid re-parsing files during analysis.
    /// </summary>
    public class AstCache
    {
        private IDictionary<string, Module> cache;
        private Analyzer analyzer;
        private IFileSystem fs;
        private string cacheDir;
        private ILogger LOG;

        public AstCache(Analyzer analyzer, IFileSystem fs, ILogger logger, string cacheDir)
        {
            this.analyzer = analyzer;
            this.fs = fs;
            this.LOG = logger;
            this.cacheDir = cacheDir;
            this.cache = new Dictionary<string, Module>();
        }

        /// <summary>
        /// Clears the memory cache.
        /// </summary>
        public void Clear()
        {
            cache.Clear();
        }

        /// <summary>
        /// Removes all serialized ASTs from the on-disk cache.
        /// </summary>
        /// <returns>
        /// true if all cached AST files were removed
        /// </returns>
        public bool clearDiskCache()
        {
            try
            {
                fs.DeleteDirectory(cacheDir);
                return true;
            }
            catch (Exception x)
            {
                LOG.Error(x, "Failed to clear disk cache. ");
                return false;
            }
        }

        public void close()
        {
            //        clearDiskCache();
        }

        /// <summary>
        /// Returns the syntax tree for <paramref name="path" />. May find and/or create a
        /// cached copy in the mem cache or the disk cache.
        /// 
        /// <param name="path">Absolute path to a source file.</param>
        /// <returns>The AST, or <code>null</code> if the parse failed for any reason</returns>
        /// </summary>
        public Module getAST(string path)
        {
            // Cache stores null value if the parse failed.
            Module module;
            if (cache.TryGetValue(path, out module))
            {
                return module;
            }

            // Might be cached on disk but not in memory.
            module = GetSerializedModule(path);
            if (module != null)
            {
                LOG.Verbose("Reusing " + path);
                cache[path] = module;
                return module;
            }

            module = null;
            try
            {
                LOG.Verbose("parsing " + path);
                var lexer = new Lexer(path, fs.CreateStreamReader(path));
                var parser = new Parser(path, lexer);
                var moduleStmts = parser.Parse().ToList();
                var posStart = moduleStmts[0].Start;
                var posEnd = moduleStmts.Last().End;
                module = new Module(
                    analyzer.moduleName(path),
                    new SuiteStatement(moduleStmts, path, posStart, posEnd),
                    path, posStart, posEnd);
            }
            finally
            {
                cache[path] = module;  // may be null
            }

            if (module != null)
            {
                serialize(module);
            }
            return module;
        }

        /// <summary>
        /// Each source file's AST is saved in an object file named for the MD5
        /// checksum of the source file.  All that is needed is the MD5, but the
        /// file's base name is included for ease of debugging.
        /// </summary>
        public string getCachePath(string sourcePath)
        {
            return fs.makePathString(cacheDir, fs.getFileHash(sourcePath));
        }

        // package-private for testing
        void serialize(Node ast)
        {
#if NEVER
        string path = getCachePath(ast.file);
        StreamWriter oos = null;
        FileOutputStream fos = null;
        try {
            fos = new StreamWriter(path);
            oos = new StreamWriter(fos);
            oos.writeObject(ast);
        } catch (Exception e) {
            _.msg("Failed to serialize: " + path);
        } finally {
            try {
                if (oos != null) {
                    oos.close();
                } else if (fos != null) {
                    fos.close();
                }
            } catch (Exception e) {
            }
        }
#endif
        }

        // package-private for testing
        internal Module GetSerializedModule(string sourcePath)
        {
            if (!File.Exists(sourcePath))
            {
                return null;
            }
            var cached = getCachePath(sourcePath);
            if (!File.Exists(cached))
            {
                return null;
            }
            return deserialize(sourcePath);
        }

        // package-private for testing
        Module deserialize(string sourcePath)
        {
#if NEVER
        string cachePath = getCachePath(sourcePath);
        FileInputStream fis = null;
        ObjectInputStream ois = null;
        try {
            fis = new FileInputStream(cachePath);
            ois = new ObjectInputStream(fis);
            return (Module) ois.readObject();
        } catch (Exception e) {
            return null;
        } finally {
            try {
                if (ois != null) {
                    ois.close();
                } else if (fis != null) {
                    fis.close();
                }
            } catch (Exception e) {

            }
        }
    }
#endif
            return null;
        }
    }
}