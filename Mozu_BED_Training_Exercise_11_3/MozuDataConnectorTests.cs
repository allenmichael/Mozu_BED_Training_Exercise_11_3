using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mozu.Api;
using Autofac;
using Mozu.Api.ToolKit.Config;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace Mozu_BED_Training_Exercise_11_3
{
    [TestClass]
    public class MozuDataConnectorTests
    {
        private IApiContext _apiContext;
        private IContainer _container;

        [TestInitialize]
        public void Init()
        {
            _container = new Bootstrapper().Bootstrap().Container;
            var appSetting = _container.Resolve<IAppSetting>();
            var tenantId = int.Parse(appSetting.Settings["TenantId"].ToString());
            var siteId = int.Parse(appSetting.Settings["SiteId"].ToString());

            _apiContext = new ApiContext(tenantId, siteId);
        }

        [TestMethod]
        public async Task Exercise_11_1_Get_Products()
        {
            //create a new product resource
            var productResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.ProductResource(_apiContext);

            //Get products
            var products = productResource.GetProductsAsync(startIndex: 0, pageSize: 200).Result;

            //Add Your Code: 
            //Write total number of products to output window
            System.Diagnostics.Debug.WriteLine("Total Products: {0}", products.TotalCount);

            //Add Your Code: 
            //Get all products that have options and are configurable
            var configurableProducts = products.Items.Where(d => d.Options != null).ToList();

            //Add Your Code: 
            //Write total number of configurable products to output window
            System.Diagnostics.Debug.WriteLine("Total Configurable Products: {0}", configurableProducts.Count);

            //Add Your Code: 
            //Get all products that do not have options and are not configurable
            var nonConfigurableProducts = products.Items.Where(d => d.Options == null).ToList();

            //Add Your Code: 
            //Write total number of non-configurable products to output window
            System.Diagnostics.Debug.WriteLine("Total Non-Configurable Products: {0}", nonConfigurableProducts.Count);

            //Add Your Code: 
            //Get all products that are scarfs
            var scarfProducts = products.Items.Where(d => d.Content.ProductName.ToLower().Contains("scarf")).ToList();

            //Add Your Code: 
            //Write total number of scarf products to output window
            System.Diagnostics.Debug.WriteLine("Total Scarf Products: {0}", scarfProducts.Count);

            //Add Your Code: 
            //Get product price
            var purseProduct = productResource.GetProductAsync("LUC-BAG-007").Result;

            //Add Your Code: 
            //Write product prices to output window
            System.Diagnostics.Debug.WriteLine("Product Prices[{0}]: Price({1}) Sales Price({2})", purseProduct.ProductCode, purseProduct.Price.Price.GetValueOrDefault().ToString("C"), purseProduct.Price.SalePrice);

            //Create a new location inventory resource
            var inventoryResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.LocationInventoryResource(_apiContext);

            //Add Your Code: 
            //Get inventory
            var inventory = inventoryResource.GetLocationInventoryAsync("WRH01", "LUC-BAG-007").Result;

            
            //Demostrate utility methods
            var collectionsList =  await StoreMultipleProductCollections(productResource);
                        
        }

        [TestMethod]
        public void Exercise_11_2_Create_New_Product()
        {
            var productCode = "LUC-BAG-011";

            //Grouped our necessary resources for defining aspects of the new product
            var productResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.ProductResource(_apiContext);
            var productTypeResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.Attributedefinition.ProductTypeResource(_apiContext);
            var categoryResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.CategoryResource(_apiContext);
            var productAttributeResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.Attributedefinition.AttributeResource(_apiContext);
            
            //Wrap the Delete call in a try/catch in case the product doesn't exist
            try
            {
                productResource.DeleteProductAsync(productCode).Wait();
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            //Retrieve the objects for later use when constructing our product
            var monogram = productAttributeResource.GetAttributeAsync("tenant~monogram").Result;
            var purseSize = productAttributeResource.GetAttributeAsync("tenant~purse-size").Result;
            var typePurse = productTypeResource.GetProductTypesAsync(filter: "Name eq 'Purse'").Result;
            var bagCategory = categoryResource.GetCategoryAsync(2).Result;

            Assert.IsNotNull(monogram);
            Assert.IsNotNull(purseSize);
            Assert.IsNotNull(typePurse);
            Assert.IsNotNull(bagCategory);

            //Define the monogram as a ProductExtra to use when defining the Product
            var monogramContract = new Mozu.Api.Contracts.ProductAdmin.ProductExtra()
            {
                AttributeFQN = monogram.AttributeFQN
            };

            //The actual List<ProductExtra> object added to the Product Contract
            var productExtraList = new List<Mozu.Api.Contracts.ProductAdmin.ProductExtra>()
            {
                monogramContract
            };

            //Construct a List<ProductOptionValue> using the purse-size Attribute Values
            var purseSizeValuesList = new List<Mozu.Api.Contracts.ProductAdmin.ProductOptionValue>();
            //Use this option to catch each Value we want to add for this Product
            var purseSizeOptionValue = new Mozu.Api.Contracts.ProductAdmin.ProductOptionValue();
            //Include only the values specified in the if-clause within the foreach loop
            foreach(var value in purseSize.VocabularyValues)
            {
                //If we wanted to include all sizes, we would remove this if-clause
                if (value.Content.StringValue.ToLower() == "petite" || value.Content.StringValue.ToLower() == "classic")
                {
                    //We instantiate a new object each time to avoid reference errors
                    purseSizeOptionValue = new Mozu.Api.Contracts.ProductAdmin.ProductOptionValue();
                    purseSizeOptionValue.AttributeVocabularyValueDetail = value;
                    purseSizeOptionValue.Value = value.Value;
                    purseSizeValuesList.Add(purseSizeOptionValue);
                }
            }

            //Define the purse-size as a ProductOption to use when defining the Product -- we use the purseSizeValuesList to add Values for the ProdcutOption
            var purseSizeContract = new Mozu.Api.Contracts.ProductAdmin.ProductOption()
            {
                AttributeFQN = purseSize.AttributeFQN,
                Values = purseSizeValuesList
            };

            //The actual Option object added to the Product Contract
            var productOptionList = new List<Mozu.Api.Contracts.ProductAdmin.ProductOption>()
            {
                purseSizeContract
            };

            //Construct a Product contract to submit to the API
            var product = new Mozu.Api.Contracts.ProductAdmin.Product()
            {
                Content = new Mozu.Api.Contracts.ProductAdmin.ProductLocalizedContent()
                {
                    ProductName = "Api Handbag",
                    LocaleCode = "en-US"
                },
                FulfillmentTypesSupported = new List<string>() 
                {
                    "DirectShip" 
                },
                HasConfigurableOptions = true,
                IsTaxable = true,
                Extras = productExtraList,
                Options = productOptionList,
                PublishingInfo = new Mozu.Api.Contracts.ProductAdmin.ProductPublishingInfo()
                {
                    PublishedState = "Live"
                },
                PackageHeight = new Mozu.Api.Contracts.Core.Measurement()
                {
                    Unit = "in",
                    Value = 7
                },
                PackageWidth = new Mozu.Api.Contracts.Core.Measurement()
                {
                    Unit = "in",
                    Value = 3
                },
                PackageLength = new Mozu.Api.Contracts.Core.Measurement()
                {
                    Unit = "in",
                    Value = 10.25m
                },
                PackageWeight = new Mozu.Api.Contracts.Core.Measurement()
                {
                    Unit = "lbs",
                    Value = 2.25m
                },
                Price = new Mozu.Api.Contracts.ProductAdmin.ProductPrice()
                {
                    Price = 175m,
                    SalePrice = 125m
                },
                ProductUsage = "Configurable",
                ProductCode = productCode,
                //Add the ProductType "purse" that we retrieved earlier
                ProductTypeId = typePurse.Items.FirstOrDefault().Id,
                HasStandAloneOptions = false,
                InventoryInfo = new Mozu.Api.Contracts.ProductAdmin.ProductInventoryInfo()
                {
                    ManageStock = true,
                    OutOfStockBehavior = "DisplayMessage"
                },
                MasterCatalogId = 1,
                ProductInCatalogs = new List<Mozu.Api.Contracts.ProductAdmin.ProductInCatalogInfo>() 
                {
                    new Mozu.Api.Contracts.ProductAdmin.ProductInCatalogInfo()
                    { 
                        CatalogId = 1,
                        IsActive = true,
                        IsContentOverridden = false,
                        IsPriceOverridden = false,
                        IsseoContentOverridden = false,
                        Content = new Mozu.Api.Contracts.ProductAdmin.ProductLocalizedContent()
                        {
                            LocaleCode = "en-US",
                            ProductName = "Api Handbag",
                        },
                        ProductCategories = new List<Mozu.Api.Contracts.ProductAdmin.ProductCategory>()
                        {
                            new Mozu.Api.Contracts.ProductAdmin.ProductCategory()
                            {
                                //Add the product to the "bag" category using what we retrieved earlier
                                CategoryId =  bagCategory.Id.Value,
                            }
                        }
                    }
                },

            };

            //The API call used to add a new product
            var newProduct = productResource.AddProductAsync(product).Result;

            var variationResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.Products.ProductVariationResource(_apiContext);


        }

        [TestMethod]
        public void Exercise_11_3_Add_Inventory_For_Product()
        {
            //Create a new location inventory resource
            var inventoryResource = new Mozu.Api.Resources.Commerce.Catalog.Admin.LocationInventoryResource(_apiContext);

            //Retrieve inventory from main warehouse
            var inventory = inventoryResource.GetLocationInventoryAsync("WRH01", "LUC-BAG-010").Result;

            var locationInventoryList = new List<Mozu.Api.Contracts.ProductAdmin.LocationInventory>()
            {
                new Mozu.Api.Contracts.ProductAdmin.LocationInventory()
                {
                    LocationCode = "WRH01", 
                    //Use the ProductVariation ProductCode here
                    ProductCode = "LUC-BAG-010-PET",
                    StockOnHand = 90
                }
            };

            //Add inventory for product in location
            var newInventory = inventoryResource.AddLocationInventoryAsync(locationInventoryList, "WRH01", true).Result;
        }

        /// <summary>
        /// Helper method for returning multiple Product Collections if the page size is greater than 1
        /// </summary>
        /// <param name="productResource">Apicontext-driven </param>
        private async static Task<List<Mozu.Api.Contracts.ProductAdmin.Product>> StoreMultipleProductCollections(Mozu.Api.Resources.Commerce.Catalog.Admin.ProductResource productResource)
        {
            var productCollectionsTaskList = new List<Task<Mozu.Api.Contracts.ProductAdmin.ProductCollection>>();
            var productCollectionsList = new List<Mozu.Api.Contracts.ProductAdmin.ProductCollection>();
            var totalProductCount = 0;
            var startIndex = 0;
            var pageSize = 1;

            var productCollection = await productResource.GetProductsAsync(pageSize: pageSize, startIndex: startIndex);
            totalProductCount = productCollection.TotalCount;
            startIndex += pageSize;
            productCollectionsList.Add(productCollection);

            while (totalProductCount > startIndex)
            {
                productCollectionsTaskList.Add(productResource.GetProductsAsync(pageSize: pageSize, startIndex: startIndex));
                startIndex += pageSize;
            }

            while(productCollectionsTaskList.Count > 0)
            {
                var finishedTask = await Task.WhenAny(productCollectionsTaskList);
                productCollectionsTaskList.Remove(finishedTask);

                productCollectionsList.Add(await finishedTask);
            }

            return ReturnProductsFromProductCollections(productCollectionsList);
        }

        /// <summary>
        /// Helper method breaking multiple ProductCollections into a List<Products>
        /// </summary>
        /// <param name="productCollectionList">A List<ProductCollection></param>
        private static List<Mozu.Api.Contracts.ProductAdmin.Product> ReturnProductsFromProductCollections(List<Mozu.Api.Contracts.ProductAdmin.ProductCollection> productCollections)
        {
            var allProducts = new List<Mozu.Api.Contracts.ProductAdmin.Product>();
            foreach (var collection in productCollections)
            {
                foreach (var product in collection.Items)
                {
                    allProducts.Add(product);
                }
            }

            return allProducts;
        }
    }
}
