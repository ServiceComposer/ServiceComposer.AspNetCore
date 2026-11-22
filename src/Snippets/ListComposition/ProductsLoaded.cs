using System.Collections.Generic;

namespace Snippets.ListComposition;

// begin-snippet: list-composition-event
public record ProductsLoaded(IReadOnlyDictionary<int, dynamic> Products);
// end-snippet
