﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectionsEducation.Redis {
	/// <summary>
	/// A Redis command object. This object represents the bytes to be sent "across the wire" including the command itself, newlines, and any arguments.
	/// </summary>
	public class Command {
		/// <summary>
		/// Byte representation.
		/// </summary>
		private readonly byte[] _bytes;

		/// <summary>
		/// Number of results to expect
		/// </summary>
		private readonly int _numberOfResultsToExpect;

		/// <summary>
		/// Gets the number of results to expect.
		/// </summary>
		public int numberOfResultsToExpect {
			get { return _numberOfResultsToExpect; }
		}

		/// <summary>
		/// Gets a command from a string.
		/// </summary>
		/// <param name="command">The command, in wire protocol form, for example <code>"PING\r\n"</code>.</param>
		/// <param name="encoding">The encoding to use for transforming string into bytes (optional: default is ASCII)</param>
		/// <returns>The <see cref="Command"/> object</returns>
		public static Command fromString(string command, Encoding encoding = null) {
			byte[] bytes = (encoding ?? Encoding.ASCII).GetBytes(command);
			return new Command(bytes);
		}

		/// <summary>
		/// Creates a command object from a byte representation.
		/// </summary>
		/// <param name="bytes">The bytes which compose the command.</param>
		private Command(byte[] bytes) {
			_bytes = bytes;
			_numberOfResultsToExpect = parseBytes(_bytes);
		}

		/// <summary>
		/// Parse the bytes given, and return the number of commands parsed.
		/// </summary>
		/// <param name="bytes">The bytes representing the commands</param>
		/// <returns>The number of commands parsed</returns>
		private static int parseBytes(byte[] bytes) {
			int commands = 0;
			ConnectionState parser = new ConnectionState();
			parser.setObjectReceivedAction(args => commands++);
			parser.buffer = bytes;
			try {
				parser.update(bytes.Length);
			} catch {
				return 1;
			}
			return commands;
		}

		/// <summary>
		/// Creates a command object from a command name and byte representation of arguments.
		/// </summary>
		/// <param name="command">The command</param>
		/// <param name="args">The arguments</param>
		public Command(string command, params byte[][] args) : this(
			new byte[][] {Encoding.ASCII.GetBytes(string.Format("${0}\r\n{1}\r\n", command.Length, command))}
				.Concat(args.Select(arg =>
					Encoding.ASCII.GetBytes(string.Format("${0}\r\n", arg.Length))
						.Concat(arg)
						.Concat(Encoding.ASCII.GetBytes("\r\n"))
					)), args.Length + 1) {}

		/// <summary>
		/// Creates a command object from a string and arguments.
		/// </summary>
		/// <param name="encoding">The encoding to use for transforming string into bytes (overloaded: default is ASCII)</param>
		/// <param name="command">The command name</param>
		/// <param name="args">The arguments</param>
		public Command(Encoding encoding, string command, params string[] args) : this(encoding, command, args, args.Length + 1) {}

		/// <summary>
		/// Creates a command object from a string and arguments.
		/// </summary>
		/// <param name="command">The command name</param>
		/// <param name="args">The arguments</param>
		public Command(string command, params string[] args) : this(Encoding.ASCII, command, args, args.Length + 1) {}

		/// <summary>
		/// Creates a command object, after being decomposed into "command parts" and "argument parts".
		/// </summary>
		/// <param name="encoding">The encoding to use for transforming string into bytes</param>
		/// <param name="command1">The command name</param>
		/// <param name="commandArgs">The arguments</param>
		/// <param name="count">The total number of arguments, including the command name itself (used for contructing "wire form" representation).</param>
		private Command(Encoding encoding, string command1, IEnumerable<string> commandArgs, int count)
			: this((command1 == null ? new string[] {} : new string[] {command1}).Concat(commandArgs).Select(arg => {
				byte[] bytes = encoding.GetBytes(arg);
				return Encoding.ASCII.GetBytes(string.Format("${0}\r\n", bytes.Length))
					.Concat(bytes)
					.Concat(Encoding.ASCII.GetBytes("\r\n"));
			}), count) {}

		/// <summary>
		/// Creates a command object, after being decomposed into "command parts" and "argument parts".
		/// </summary>
		/// <param name="commandArgs">The arguments</param>
		/// <param name="count">The total number of arguments, including the command name itself (used for contructing "wire form" representation).</param>
		private Command(IEnumerable<IEnumerable<byte>> commandArgs, int count) {
			IEnumerable<byte> commands = commandArgs.Aggregate(Enumerable.Empty<byte>(), (all, bytes) => all.Concat(bytes));
			_bytes = Encoding.ASCII.GetBytes(string.Format("*{0}\r\n", count))
				.Concat(commands).ToArray();
			_numberOfResultsToExpect = parseBytes(_bytes);
		}

		/// <summary>
		/// Gets the byte representation of the command, for sending "over the wire".
		/// </summary>
		/// <returns>A byte array, representing the command in wire-protocol format.</returns>
		public byte[] getBytes() {
			byte[] bytes = (byte[])_bytes.Clone();
			return bytes;
		}
	}
}