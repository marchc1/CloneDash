using SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Util;

public class NucleusEngineException(string message) : Exception(message) {
	public static unsafe NucleusEngineException Show(string message){
		SDL3.SDL_ShowSimpleMessageBox(SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "Nucleus Engine - Fatal Exception", message, null);

		return new NucleusEngineException(message);
	}

}