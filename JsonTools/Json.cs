using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System;


namespace JsonTools {
	/// <summary>
	/// Json
	/// </summary>
	public static class Json {
		/// <summary>Build JSON string</summary>
		/// <param name="obj">Object</param>
		/// <returns>JSON string</returns>
		public static string Build(object obj){
			/// IT'S A NULL
			if(obj == null) return "null";


			// Write JSON string
			var writeJson = new StringWriter();

			// Object type
			Type type = obj.GetType();


			/// IT'S A BOOLEAN
			if(type == typeof(bool)){
				if((bool)obj == true) writeJson.Write("true");
				else writeJson.Write("false");
			}

			/// IT'S A ENUM
			else if(type.IsEnum){
				var e = Enum.GetName(type, obj);
				writeJson.Write("\"{0}\"", e);
			}

			/// IT'S A STRING
			else if(
				type == typeof(char) ||
				type == typeof(string)
			) writeJson.Write("\"{0}\"", obj);

			/// IT'S A NUMBER
			else if(
				type == typeof(byte)   || type == typeof(sbyte)  ||
				type == typeof(short)  || type == typeof(ushort) ||
				type == typeof(int)    || type == typeof(uint)   ||
				type == typeof(long)   || type == typeof(ulong)  ||
				type == typeof(float)  || type == typeof(double) ||
				type == typeof(decimal)
			){
				var number = Convert.ToString(obj);
				writeJson.Write(number.Replace(',', '.'));
			}

			/// IT'S A DICTIONARY
			else if (
				!type.IsArray &&
				typeof(IDictionary).IsAssignableFrom(type)
			){
				writeJson.Write("{");

				var e = ((IDictionary)obj).GetEnumerator();

				bool isFirst = true;
				while(e.MoveNext()){
					// Valid key
					if(e.Key is string){
						if (isFirst) isFirst = false;
						else writeJson.Write(",");

						writeJson.Write("\"{0}\":", e.Key);
						writeJson.Write(Build(e.Value));
					}

					// Invalid key
					else return "";
				}

				writeJson.Write("}");
			}

			/// IT'S A ARRAY OR LIST
			else if (
				type.IsArray ||
				typeof(IList).IsAssignableFrom(type)
			){
				writeJson.Write("[");

				IEnumerator item;
				if (type.IsArray) item = ((Array)obj).GetEnumerator();
				else item = ((IList)obj).GetEnumerator();

				bool isFirst = true;
				while(item.MoveNext()){
					if(isFirst) isFirst = false;
					else writeJson.Write(",");

					writeJson.Write(Build(item.Current));
				}

				writeJson.Write("]");
			}

			/// IT'S A OBJECT
			else if (
				!type.IsArray &&
				!type.IsPrimitive &&
				type != typeof(string) &&
				type != typeof(decimal)
			){
				writeJson.Write("{");

				// First item
				bool isFirst = true;

				// Fields info
				FieldInfo[] fields = type.GetFields();
				for(int i = 0; i < fields.Length; i++){
					if(
						 fields[i].IsPublic &&
						!fields[i].IsStatic &&
						!fields[i].IsNotSerialized
					){
						if(isFirst) isFirst = false;
						else writeJson.Write(",");

						writeJson.Write("\"{0}\":", fields[i].Name);
						writeJson.Write(Build(fields[i].GetValue(obj)));
					}
				}

				// Properties info
				PropertyInfo[] properties = type.GetProperties();
				for(int i = 0; i < properties.Length; i++){
					if(
						properties[i].CanRead &&
						properties[i].CanWrite
					){
						if(isFirst) isFirst = false;
						else writeJson.Write(",");

						writeJson.Write("\"{0}\":", properties[i].Name);
						writeJson.Write(Build(properties[i].GetValue(
							obj, BindingFlags.Default,
							null, null, null
						)));
					}
				}

				writeJson.Write("}");
			}


			// Excepition non-serializable type
			else throw new Exception("Non-serializable type " + type);

			// JSON string
			return writeJson.ToString();
		}

		/// <summary>Parse JSON</summary>
		/// <param name="json">JSON</param>
		/// <typeparam name="O">Object</typeparam>
		public static O Parse<O>(string json){
			// Indexer
			int i = 0;

			//try {
			// Process value
			var result = ProcessValue(json, ref i, typeof(O));

			// Uncomplete
			if(i < json.Length)
			return default(O);

			// Result
			return (O)result;
			//}

			//catch(Exception e){
			//	// Return object
			//	return default(O);
			//}
		}


		/// <summary>Processes JSON</summary>
		/// <param name="json">JSON</param>
		/// <param name="i">Indexer</param>
		/// <param name="type">Type</param>
		/// <returns>Object</returns>
		private static object ProcessValue(string json, ref int i, Type type){
			// JSON empty
			if(string.IsNullOrEmpty(json))
			return null;

			// Length
			int length = json.Length;

			// Exception
			if(i >= length)
			throw new Exception("Syntax error");


			/// IT'S A NULL
			if(json[i] == 'n'){
				// NULL
				if (
					json[i + 1] == 'u' &&
					json[i + 2] == 'l' &&
					json[i + 3] == 'l'
				){
					i += 4;
					return null;
				}

				// Exception
				throw new Exception("Syntax error");
			}

			/// IT'S A BOOLEAN
			if(
				type == typeof(bool) && (
					json[i] == 'f' ||
					json[i] == 't'
				)
			){
				// FALSE
				if(
					json[i + 1] == 'a' &&
					json[i + 2] == 'l' &&
					json[i + 3] == 's' &&
					json[i + 4] == 'e'
				){
					i += 5;
					return false;
				}

				// TRUE
				if (
					json[i + 1] == 'r' &&
					json[i + 2] == 'u' &&
					json[i + 4] == 'e'
				){
					i += 4;
					return true;
				}

				// Exception
				throw new Exception("Syntax error");
			}

			/// IT'S A ENUM
			if (
				type.IsEnum &&
				json[i] == '"'
			){
				var enumerator = ProcessString(json, ref i);
				return Enum.Parse(type, enumerator);
			}

			/// IT'S A STRING
			if (
				type == typeof(string) &&
				json[i] == '"'
			){return ProcessString(json, ref i);}

			/// IT'S A NUMBER
			if(
				(
					json[i] == '-' ||
					json[i] == '+' ||
					(
						json[i] >= '0' &&
						json[i] <= '9'
					)
				) && (
					type == typeof(byte)   || type == typeof(sbyte)  ||
					type == typeof(short)  || type == typeof(ushort) ||
					type == typeof(int)    || type == typeof(uint)   ||
					type == typeof(long)   || type == typeof(ulong)  ||
					type == typeof(float)  || type == typeof(double) ||
					type == typeof(decimal)
				)
			){return ProcessNumber(json, ref i, type);}

			/// IT'S A DICTIONARY
			if(
				!type.IsArray &&
				typeof(IDictionary).IsAssignableFrom(type) &&
				json[i] == '{'
			){
				i++;

				// Dictionary
				var dict = new Dictionary<string, object>();

				// End
				if(json[i] == '}')
				{i++; return dict;}

				while(i < length){
					// Key
					var key = ProcessString(json, ref i);

					// Exception expected :
					if (json[i++] != ':')
						throw new Exception("Expected ':' on " + (i - 1));

					// Process value
					Type[] types = type.GetGenericArguments();
					var value = ProcessValue(json, ref i, types[1]);

					// Add
					dict.Add(key, value);

					// End
					if(json[i] == '}')
					{i++; return dict; }

					// Exception expected ,
					if(json[i++] != ',')
					throw new Exception("Expected ',' on " + (i - 1));
				}

				// Exception
				throw new Exception("Syntax error");
			}

			/// IT'S A ARRAY OR LIST
			if(
				json[i] == '[' && (
					type.IsArray ||
					typeof(IList).IsAssignableFrom(type)
				)
			){
				i++;

				// Dynamic array
				var dya = new List<object>();

				// End
				if(json[i] == ']'){
					i++;

					if (typeof(IList).IsAssignableFrom(type)) return dya;
					else return dya.ToArray();
				}

				while(i < length){
					// Element type
					Type elementType;
					if(typeof(IList).IsAssignableFrom(type) && type.IsGenericType){
						Type[] generic = type.GetGenericArguments();
						elementType = generic[0];
					}
					else elementType = type.GetElementType();

					// Process value
					dya.Add(ProcessValue(json, ref i, elementType));

					// End
					if(json[i] == ']'){
						i++;

						if(typeof(IList).IsAssignableFrom(type)) return dya;
						else return dya.ToArray();
					}

					// Exception expected ,
					if (json[i++] != ',')
						throw new Exception("Expected ',' on " + (i - 1));
				}

				// Exception
				throw new Exception("Syntax error");
			}

			/// IT'S A OBJECT
			if(
				!type.IsArray &&
				!type.IsPrimitive &&
				type != typeof(string) &&
				type != typeof(decimal) &&
				json[i] == '{'
			){
				i++;

				// Object
				object obj = null;
				if(type.IsValueType) obj = Activator.CreateInstance(type);
				else obj = FormatterServices.GetUninitializedObject(type);

				// End
				if(json[i] == '}')
				{i++; return obj;}

				while(i < length){
					// Field or Property name
					var name = ProcessString(json, ref i);

					// Exception expected :
					if(json[i++] != ':')
					throw new Exception("Expected ':' on " + (i - 1));

					// Process field
					var field = type.GetField(name);
					if(field != null) field.SetValue(obj, ProcessValue(json, ref i, field.FieldType));

					// Process property
					var property = type.GetProperty(name);
					if(property != null) property.SetValue(obj, ProcessValue(json, ref i, property.PropertyType));

					// End
					if(json[i] == '}')
					{i++; return obj;}

					// Exception expected ,
					if(json[i++] != ',')
					throw new Exception("Expected ',' on " + (i - 1));
				}

				// Exception
				throw new Exception("Syntax error");
			}

			// Exception
			throw new Exception("Syntax error");
		}

		/// <summary>Processes string</summary>
		/// <param name="json">JSON</param>
		/// <param name="i">indexer</param>
		/// <returns>string</returns>
		private static string ProcessString(string json, ref int i){
			// Length
			int length = json.Length;

			if(
				i < length &&
				json[i - 1] != '\\' &&
				json[i++] == '"'
			){
				var builder = new StringBuilder();

				while(
					i < length && (
						json[i - 1] == '\\' ||
						json[i] != '"'
					)
				){
					if(json[i] == '\\'){
						i++;

						switch(json[i]){
							case '"':
								builder.Append('"');
								break;
							case 'n':
								builder.Append('\n');
								break;
							case 'r':
								builder.Append('\r');
								break;
							case 't':
								builder.Append('\t');
								break;
						}
					}
					else builder.Append(json[i]);

					i++;
					if(i >= json.Length)
					break;
				}

				i++;
				return builder.ToString();
			}

			// Exception
			throw new Exception("Syntax error");
		}

		/// <summary>Processes number</summary>
		/// <param name="json">JSON</param>
		/// <param name="i">Indexer</param>
		/// <param name="type">Type</param>
		/// <returns>Object</returns>
		private static object ProcessNumber(string json, ref int i, Type type){
			// Lenght
			var length = json.Length;

			// To create the number
			var builder = new StringBuilder();

			// Signal
			if(
				i < length && (
					json[i] == '-' ||
					json[i] == '+'
				)
			){
				builder.Append(json[i]);
				i++;
			}

			if(
				i < length &&
				json[i] >= '0' &&
				json[i] <= '9'
			){
				// Digit
				while(
					i < length &&
					json[i] >= '0' &&
					json[i] <= '9'
				){
					builder.Append(json[i]);
					i++;
				}

				// Decimal separator
				if(
					i < length &&
					json[i] == '.'
				){
					builder.Append(json[i]);
					i++;

					// Digit
					while(
		 				i < length &&
		 				json[i] >= '0' &&
		 				json[i] <= '9'
		 			){
						builder.Append(json[i]);
						i++;
					}

					// Notation
					if(
		 				i < length && (
		 					json[i] == 'e' ||
							json[i] == 'E'
		 				)
					){
						builder.Append(json[i]);
						i++;

						// Signal
						if(
							i < length && (
								json[i] == '-' ||
								json[i] == '+'
							)
						){
							builder.Append(json[i]);
							i++;
						}

						// Digit
						while(
							i < length &&
							json[i] >= '0' &&
							json[i] <= '9'
						){
							builder.Append(json[i]);
							i++;
						}
					}
				}

				return Convert.ChangeType(
					builder.ToString(),
					type
				);
			}

			// Exception
			throw new Exception("Syntax error");
		}
	};
};
