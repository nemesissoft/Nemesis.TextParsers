# Todo / road map
## Add support for:
- [ ] parsing value tuples (ITuple or concrete value)
- [x] parsing custom dictionaries (i.e. StringKeyedDictionary&lt;int&gt;: Dictionary&lt;string, int&gt;{})

## Bigger tasks
1. context based transformer creation with settings for:
	* DictionaryBehaviour
    * Enum casing+other customizations
    * empty string meaing (empty, default, null?))
2. custom TextParser factory/customizations