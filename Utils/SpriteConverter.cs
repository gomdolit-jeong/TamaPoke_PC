using System;
using System.Collections.Generic;
using System.Drawing; // NuGet 패키지 관리자에서 'System.Drawing.Common' 설치 필요
using System.IO;
using System.Text.Json;
using System.Xml;

namespace TamaPoke.Utils
{
	public class SpriteConverter
	{
		// 🌟 용량 최적화를 위한 '블랙리스트' 필터
		// 이 배열에 포함된 단어가 폼 이름에 들어가면 변환을 건너뜁니다.
		private static readonly string[] BlackListForms = new string[]
		{
			"altcolor", "alternate", "cutscene", "turban", "substitute", "beta",
			"mikon", "hakogame", "norowara", "ubausagi", "warabbit", "animon", "gecqua", "browt", "pombon"
		};

		/// <summary>
		/// 안전하게 모든 하위 폴더를 탐색하며 애니메이션 에셋을 압축된 .bin 파일로 구워냅니다.
		/// </summary>
		public static string BatchConvertAll(string pmdSpriteRootFolder, string outputFolder, string trackerJsonPath)
		{
			if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

			if (!File.Exists(trackerJsonPath))
				return $"[에러] tracker.json 파일을 찾을 수 없습니다.\n경로를 다시 확인해주세요:\n{trackerJsonPath}";

			JsonElement root;
			try
			{
				string jsonText = File.ReadAllText(trackerJsonPath);
				using JsonDocument doc = JsonDocument.Parse(jsonText);
				root = doc.RootElement.Clone();
			}
			catch (Exception ex)
			{
				return $"[에러] tracker.json 로드 실패:\n{ex.Message}";
			}

			List<string> allXmlFiles = GetAllXmlFilesSafe(pmdSpriteRootFolder);
			if (allXmlFiles.Count == 0)
				return $"[에러] '{pmdSpriteRootFolder}' 내부에 AnimData.xml 파일이 하나도 없습니다!\n경로를 확인해주세요.";

			int successCount = 0;
			int skipCount = 0;
			int failCount = 0;

			foreach (string xmlPath in allXmlFiles)
			{
				string? folderPath = Path.GetDirectoryName(xmlPath);
				if (string.IsNullOrEmpty(folderPath)) continue;

				string relativePath = Path.GetRelativePath(pmdSpriteRootFolder, folderPath);
				string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string dexIdStr = pathParts[0];

				string formSuffix = GetFormSuffixFromJson(root, pathParts);

				// 블랙리스트 검사 (대소문자 무시)
				bool shouldSkip = false;
				string lowerSuffix = formSuffix.ToLower();

				foreach (string blackWord in BlackListForms)
				{
					if (lowerSuffix.Contains(blackWord))
					{
						shouldSkip = true;
						break;
					}
				}

				if (shouldSkip)
				{
					skipCount++;
					continue;
				}

				string fileName = $"p{dexIdStr}{formSuffix}.bin";
				string outputBinPath = Path.Combine(outputFolder, fileName);

				bool isSuccess = AutoConvertPmdToBin(folderPath, xmlPath, outputBinPath);
				if (isSuccess) successCount++;
				else failCount++;
			}

			return $"🎉 변환 완료!\n총 {successCount}개의 에셋을 압축하여 저장했습니다.\n(필터링으로 {skipCount}개의 폼을 걸러냈습니다!)\n(실패: {failCount}개)";
		}

		/// <summary>
		/// 권한 오류 등을 무시하고 깊은 경로까지 멈추지 않고 탐색합니다.
		/// </summary>
		private static List<string> GetAllXmlFilesSafe(string rootPath)
		{
			List<string> files = new List<string>();
			try
			{
				files.AddRange(Directory.GetFiles(rootPath, "AnimData.xml"));
				foreach (string dir in Directory.GetDirectories(rootPath))
				{
					files.AddRange(GetAllXmlFilesSafe(dir));
				}
			}
			catch { /* 접근 불가 폴더는 무시 */ }
			return files;
		}

		/// <summary>
		/// tracker.json의 subgroups 구조를 추적하여 읽기 쉬운 폼 이름을 생성합니다.
		/// </summary>
		private static string GetFormSuffixFromJson(JsonElement root, string[] pathParts)
		{
			if (pathParts.Length <= 1) return "";

			List<string> formNames = new List<string>();
			JsonElement currentElement = root;

			if (!currentElement.TryGetProperty(pathParts[0], out currentElement))
				return GetFallbackNumberSuffix(pathParts);

			for (int i = 1; i < pathParts.Length; i++)
			{
				if (currentElement.TryGetProperty("subgroups", out JsonElement subgroups) &&
					subgroups.TryGetProperty(pathParts[i], out JsonElement nextNode))
				{
					currentElement = nextNode;
					if (currentElement.TryGetProperty("name", out JsonElement nameElement))
					{
						string? formName = nameElement.GetString();
						if (!string.IsNullOrEmpty(formName))
						{
							formNames.Add(formName.Replace(" ", "_"));
						}
					}
				}
				else
				{
					return GetFallbackNumberSuffix(pathParts);
				}
			}

			if (formNames.Count > 0)
				return "_" + string.Join("_", formNames);

			return GetFallbackNumberSuffix(pathParts);
		}

		private static string GetFallbackNumberSuffix(string[] pathParts)
		{
			List<string> rawNumbers = new List<string>();
			for (int i = 1; i < pathParts.Length; i++)
			{
				if (pathParts[i] != "0000") rawNumbers.Add(pathParts[i]);
			}
			return rawNumbers.Count > 0 ? "_" + string.Join("_", rawNumbers) : "";
		}

		/// <summary>
		/// PNG 이미지를 MemoryStream을 활용하여 압축 상태 그대로 이진 파일(.bin)로 저장합니다.
		/// </summary>
		private static bool AutoConvertPmdToBin(string folderPath, string xmlFilePath, string outputBinPath)
		{
			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(xmlFilePath);

				XmlNodeList? animNodes = doc.SelectNodes("//Anim");
				if (animNodes == null || animNodes.Count == 0) return false;

				// Index를 키로, (FrameWidth, FrameHeight, 이미지경로)를 저장
				Dictionary<int, (int FW, int FH, string PngPath)> animDataDict = new();
				int maxIndex = 0;

				foreach (XmlNode animNode in animNodes)
				{
					XmlNode? indexNode = animNode.SelectSingleNode("Index");
					if (indexNode == null) continue;
					int index = int.Parse(indexNode.InnerText);

					// CopyOf 처리를 위해 실제 타겟 노드 찾기
					string targetAnimName = animNode.SelectSingleNode("CopyOf")?.InnerText
											?? animNode.SelectSingleNode("Name")?.InnerText ?? "";

					XmlNode? sourceNode = animNode.SelectSingleNode("CopyOf") != null
						? doc.SelectSingleNode($"//Anim[Name='{targetAnimName}']")
						: animNode;

					int fw = 0, fh = 0;
					if (sourceNode != null)
					{
						// 🌟 AnimData.xml에 명시된 완벽한 프레임 사이즈를 가져옵니다!
						fw = int.Parse(sourceNode.SelectSingleNode("FrameWidth")?.InnerText ?? "0");
						fh = int.Parse(sourceNode.SelectSingleNode("FrameHeight")?.InnerText ?? "0");
					}

					string pngPath = Path.Combine(folderPath, $"{targetAnimName}-Anim.png");

					if (File.Exists(pngPath) && fw > 0 && fh > 0)
					{
						animDataDict[index] = (fw, fh, pngPath);
						if (index > maxIndex) maxIndex = index;
					}
				}

				if (animDataDict.Count == 0) return false;
				int totalStrips = maxIndex + 1;

				using (BinaryWriter writer = new BinaryWriter(File.Open(outputBinPath, FileMode.Create)))
				{
					writer.Write(totalStrips);

					for (int i = 0; i < totalStrips; i++)
					{
						if (animDataDict.TryGetValue(i, out var data))
						{
							using (Bitmap strip = new Bitmap(data.PngPath))
							using (MemoryStream ms = new MemoryStream())
							{
								strip.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
								byte[] pngBytes = ms.ToArray();

								// 🌟 픽셀 데이터 전에 정확한 크기 정보를 먼저 기록합니다.
								writer.Write(data.FW);
								writer.Write(data.FH);
								writer.Write(pngBytes.Length);
								writer.Write(pngBytes);
							}
						}
						else
						{
							writer.Write(0); // FW
							writer.Write(0); // FH
							writer.Write(0); // Length
						}
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}