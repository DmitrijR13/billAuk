using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;

namespace Bars.QTask.Extensions
{
    public static class MefExtensions
    {
        public static IEnumerable<KeyValuePair<Type, M>> GetExportTypesWithMetadata<T, M>(
            this CompositionContainer mefcontainer)
            where T : class
            where M : class
        {

            // need to examine each type to see if they have the correct export attribute and metadata
            foreach (var type in mefcontainer.GetExportTypes<T>())
            {
                // should just be one if more than one will throw exception
                // metadata or export attribute has to implement the interface
                var metadataAttribute =
                    type.GetCustomAttributes(false)
                        .SelectMany(
                            a =>
                            a.GetType()
                             .GetCustomAttributes(false)
                             .OfType<MetadataAttributeAttribute>()
                             .Concat<Attribute>(new[] { a }.OfType<ExportAttribute>()))
                        .OfType<M>().SingleOrDefault();

                // if we found the correct metadata
                if (metadataAttribute != null)
                {
                    // return the lazy factory
                    yield return new KeyValuePair<Type, M>(type, metadataAttribute);
                }
            }
        }

        public static IEnumerable<Type> GetExportTypes<T>(this CompositionContainer mefContainer)
            where T : class
        {
            // look in the mef catalog to grab out all the types that are of type T
            return mefContainer.Catalog.Parts.Where(part => part.ExportDefinitions
                                                                .Any(
                                                                    def =>
                                                                    def.Metadata.ContainsKey(
                                                                        "ExportTypeIdentity") &&
                                                                    def.Metadata["ExportTypeIdentity"]
                                                                        .Equals(
                                                                            typeof(T).FullName)))
                               .AsEnumerable()
                               .Select(part => ReflectionModelServices.GetPartType(part).Value);
        }
    }
}
