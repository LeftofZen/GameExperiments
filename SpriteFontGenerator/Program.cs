// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var dir = @"C:\\Users\\bigba\\source\\repos\\Experiments\\Fonts\\Content\\Fonts";

var ttf = Directory.GetFiles(dir, "*.ttf");
var otf = Directory.GetFiles(dir, "*.otf");

const string template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<XnaContent xmlns:Graphics=""Microsoft.Xna.Framework.Content.Pipeline.Graphics"">
  <Asset Type=""Graphics:FontDescription"">
    <FontName>__fontname__</FontName>
    <Size>48</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#126;</End>
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
";

foreach (var file in ttf
	.Concat(otf)
	.DistinctBy(x => Path.GetFileNameWithoutExtension(x))
	.Order())
{
	var fontName = Path.GetFileNameWithoutExtension(file);
	var _ = Path.Combine(dir, file);
	//Console.WriteLine($"TTF Font: {fontName}, Path: {fontPath}");

	var variableName = fontName
		.Replace(' ', '_')
		.Replace('-', '_')
		.Replace("-", "")
		.Replace("[", "")
		.Replace("]", "")
		.Replace("(", "")
		.Replace(")", "")
		.Replace("{", "")
		.Replace("}", "")
		.Replace("'", "")
		.Replace("!", "");

	var newFile = template.Replace("__fontname__", fontName);
	File.WriteAllText(Path.Combine(dir, $"{fontName}.spritefont"), newFile);
	Console.WriteLine($"public const string _{variableName} = \"{fontName}\";");
}

Console.ReadLine();
