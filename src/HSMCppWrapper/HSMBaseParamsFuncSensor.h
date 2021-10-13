#pragma once

#include "msclr/auto_gcroot.h"
#include "msclr/marshal_cppstd.h"

using System::Func;
using System::String;
using System::Collections::Generic::List;

namespace hsm_wrapper
{

	template<class T, class U>
	class HSMBaseParamsFuncSensor;

	template<class T, class U>
	ref class ParamsFuncDelegateWrapper
	{
	public:
		ParamsFuncDelegateWrapper(HSMBaseParamsFuncSensor<T, U>* base) : base(base)
		{
		}

		template<class X = T, class Y = U>
		typename std::enable_if_t<std::is_arithmetic_v<X> && std::is_arithmetic_v<Y>, X>
			Call(List<Y>^ values)
		{
			try
			{
				std::list<U> converted_values;
				for each(U value in values)
				{
					converted_values.push_back(value);
				}
				return base->Func(converted_values);
			}
			catch (std::exception& ex)
			{
				throw gcnew Exception(gcnew String(ex.what()));
			}
		}

		template<class X = T, class Y = U>
		typename std::enable_if_t<std::is_same_v<X, std::string> && std::is_arithmetic_v<Y>, String^>
			Call(List<Y>^ values)
		{
			try
			{
				std::list<U> converted_values;
				for each(U value in values)
				{
					converted_values.push_back(value);
				}
				return gcnew String(base->Func(converted_values).c_str());
			}
			catch (std::exception& ex)
			{
				throw gcnew Exception(gcnew String(ex.what()));
			}
		}

		template<class X = T, class Y = U>
		typename std::enable_if_t<std::is_arithmetic_v<X> && std::is_same_v<Y, std::string>, X>
			Call(List<String^>^ values)
		{
			try
			{
				std::list<std::string> converted_values;
				for each(String^ value in values)
				{
					converted_values.push_back(move(msclr::interop::marshal_as<std::string>(value)));
				}
				return base->Func(converted_values);
			}
			catch (std::exception& ex)
			{
				throw gcnew Exception(gcnew String(ex.what()));
			}
		}

		template<class X = T, class Y = U>
		typename std::enable_if_t<std::is_same_v<X, std::string> && std::is_same_v<Y, std::string>, String^>
			Call(List<String^>^ values)
		{
			try
			{
				std::list<std::string> converted_values;
				for each(String^ value in values)
				{
					converted_values.push_back(move(msclr::interop::marshal_as<std::string>(value)));
				}
				return gcnew String(base->Func(converted_values).c_str());
			}
			catch (std::exception& ex)
			{
				throw gcnew Exception(gcnew String(ex.what()));
			}
		}

	private:
		HSMBaseParamsFuncSensor<T, U>* base;
	};

	template<class T, class U>
	class HSMBaseParamsFuncSensor
	{
	public:
		ParamsFuncDelegateWrapper<T, U>^ GetDelegateWrapper();
		void SetFunc(std::function<T(std::list<U>)> function);
		T Func(const std::list<U>& values);

	protected:
		HSMBaseParamsFuncSensor();

	private:
		std::function<T(std::list<U>)> func;
		msclr::auto_gcroot<ParamsFuncDelegateWrapper<T, U>^> delegate_wrapper;
	};

}