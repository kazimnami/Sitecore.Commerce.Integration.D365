using Sitecore.Commerce.Core;
using Sitecore.Commerce.Core.Commands;
using Sitecore.Commerce.Plugin.Catalog;
using Sitecore.Commerce.Plugin.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleIntegrationD365.Feature.Inventory.Engine
{
    public class GetCatalogContextCommand : CommerceCommand
    {
        public GetCatalogContextCommand(IServiceProvider serviceProvider) : base(serviceProvider) { }

        public async Task<IEnumerable<CatalogContextModel>> Process(CommerceContext commerceContext, IEnumerable<string> catalogNameList, bool useCache = true)
        {
            using (CommandActivity.Start(commerceContext, this))
            {
                if (catalogNameList == null || catalogNameList.Count().Equals(0)) throw new Exception($"{nameof(GetCatalogContextCommand)} no catalogs provided");

                if(useCache == false)
                {
                    commerceContext.RemoveObjects<CatalogContextModel>();
                }

                var allCatalogs = commerceContext.GetObject<IEnumerable<Sitecore.Commerce.Plugin.Catalog.Catalog>>();
                if (allCatalogs == null)
                {
                    allCatalogs = await Command<GetCatalogsCommand>().Process(commerceContext);
                    commerceContext.AddObject(allCatalogs);
                }

                var catalogCategoryModelList = new List<CatalogContextModel>();
                foreach (var catalogName in catalogNameList)
                {
                    var model = commerceContext.GetObject<CatalogContextModel>(m => m.CatalogName.Equals(catalogName));
                    if (model == null)
                    {
                        var catalog = allCatalogs.FirstOrDefault(c => c.Name.Equals(catalogName));
                        var allCategories = (await Command<GetCategoriesCommand>().Process(commerceContext, catalogName)).ToList();
                        if (allCategories == null) allCategories = new List<Category>();

                        allCategories = allCategories.Where(c => c.Id.CatalogNameFromCategoryId().Equals(catalogName)).ToList();

                        model = new CatalogContextModel
                        {
                            CatalogName = catalog.Name,
                            Catalog = catalog,
                            CategoriesByName = allCategories.ToDictionary(c => c.Name),
                            CategoriesBySitecoreId = allCategories.ToDictionary(c => c.SitecoreId)
                        };

                        commerceContext.AddObject(model);
                    }

                    catalogCategoryModelList.Add(model);
                }

                return catalogCategoryModelList;
            }
        }
    }
}
