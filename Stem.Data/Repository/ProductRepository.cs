using Stem.Data.Base;
using Stem.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.Repository
{
    public class ProductRepository : GenericRepository<Product>
    {
        public ProductRepository() { }

        public ProductRepository(STEMKITshopDBContext context) => _context = context;
    }
}
