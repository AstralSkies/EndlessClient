﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

namespace EOLib.Domain
{
	public interface INumberEncoderService
	{
		byte[] EncodeNumber(int number, int size);

		int DecodeNumber(params byte[] b);
	}
}