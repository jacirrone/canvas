using Canvas.CommandLineParsing.OptionProcessing;
using Isas.Shared;

namespace Canvas.CommandLineParsing.CoreOptionTypes
{
    public class DirectoryOption : ValueOption<IDirectoryLocation>
    {
        private DirectoryOption(ValueOptionInfo<string> info) : base(info)
        {
        }

        public static new DirectoryOption CreateRequired(string description, params string[] names)
        {
            return new DirectoryOption(new RequiredValueOptionInfo(description, names));
        }
        public static new DirectoryOption Create(string description, params string[] names)
        {
            return new DirectoryOption(new ValueOptionInfo<string>(false, description, names));
        }

        public override ParsingResult<IDirectoryLocation> Parse(string value)
        {
            IDirectoryLocation location = value == null ? null : new DirectoryLocation(value);
            if (location == null || location.Exists)
                return ParsingResult<IDirectoryLocation>.SuccesfulResult(location);
            return ParsingResult<IDirectoryLocation>.FailedResult($"Error: {location} does not exist");
        }
    }
}