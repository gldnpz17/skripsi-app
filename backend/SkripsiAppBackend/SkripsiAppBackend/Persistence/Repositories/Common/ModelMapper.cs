namespace SkripsiAppBackend.Persistence.Repositories.Common
{
    static class ModelMapper
    {
        public static List<TTarget> MapTo<TTarget>(this List<dynamic> sources)
        {
            if (sources == null)
            {
                return null;
            }

            List<TTarget> targets = new();

            sources.ForEach(source =>
            {
                if (source == null)
                {
                    targets.Add(source);
                }

                targets.Add(ModelMapper.MapTo<TTarget>(source));
            });

            return targets;
        }

        public static TTarget MapTo<TTarget>(dynamic source)
        {
            var targetType = typeof(TTarget);

            var targetConstructor = targetType.GetConstructor(Array.Empty<Type>());

            if (targetConstructor == null) throw new Exception($"Default constructor for type {targetType.Name} not found.");

            var targetProperties = targetType.GetProperties();

            var sourceDictionary = (IDictionary<string, object>)source;

            var target = targetConstructor.Invoke(Array.Empty<object>());

            foreach (var targetProperty in targetProperties)
            {
                var value = sourceDictionary[targetProperty.Name.CamelCaseToSnakeCase()];
                targetProperty.SetValue(target, value);
            }

            return (TTarget)target;
        }

        private static string CamelCaseToSnakeCase(this string source)
        {
            return source
                .Select((character, index) => (index, character))
                .Aggregate("", (target, letter) =>
                {
                    var isFirstLetter = letter.index == 0;
                    var isLastLetter = letter.index == source.Length - 1;
                    var previousLetterIsUpperCase = !isFirstLetter && char.IsUpper(source[letter.index - 1]);
                    var nextLetterIsLowerCase = !isLastLetter && char.IsLower(source[letter.index + 1]);

                    var isPrecededByUnderscore = !isFirstLetter && (!previousLetterIsUpperCase || (previousLetterIsUpperCase && nextLetterIsLowerCase));

                    if (char.IsUpper(letter.character))
                    {
                        return $"{target}{(isPrecededByUnderscore ? "_" : "")}{char.ToLower(letter.character)}";
                    }

                    return $"{target}{letter.character}";
                });
        }
    }
}
