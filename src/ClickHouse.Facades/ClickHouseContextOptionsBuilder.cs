﻿using ClickHouse.Facades.Utility;

namespace ClickHouse.Facades;

public sealed class ClickHouseContextOptionsBuilder<TContext>
	: Builder<ClickHouseContextOptions<TContext>, ClickHouseContextOptionsBuilder<TContext>>
	where TContext : ClickHouseContext<TContext>
{
	private const string UseSessionParameterName = "UseSession";

	private OptionalValue<string> _connectionString;
	private OptionalValue<bool> _forceSession;
	private OptionalValue<ClickHouseFacadeRegistry<TContext>> _facadeRegistry;
	private OptionalValue<bool> _allowDatabaseChanges;
	private OptionalValue<HttpClient> _httpClient;
	private OptionalValue<IHttpClientFactory> _httpClientFactory;
	private OptionalValue<string> _httpClientName;

	public ClickHouseContextOptionsBuilder<TContext> WithHttpClientFactory(
		IHttpClientFactory httpClientFactory,
		string httpClientName)
	{
		ExceptionHelpers.ThrowIfNull(httpClientFactory);
		ExceptionHelpers.ThrowIfNull(httpClientName);

		if (_httpClient.HasValue)
		{
			throw new InvalidOperationException(
				"Unable to set IHttpClientFactory and HttpClient simultaneously.");
		}

		WithPropertyValue(
			builder => builder._httpClientName,
			(builder, value) => builder._httpClientName = value,
			httpClientName);

		return WithPropertyValue(
			builder => builder._httpClientFactory,
			(builder, value) => builder._httpClientFactory = value,
			httpClientFactory);
	}

	public ClickHouseContextOptionsBuilder<TContext> WithHttpClient(HttpClient httpClient)
	{
		ExceptionHelpers.ThrowIfNull(httpClient);

		if (_httpClientFactory.HasValue)
		{
			throw new InvalidOperationException(
				"Unable to set IHttpClientFactory and HttpClient simultaneously.");
		}

		return WithPropertyValue(
			builder => builder._httpClient,
			(builder, value) => builder._httpClient = value,
			httpClient);
	}

	public ClickHouseContextOptionsBuilder<TContext> AllowDatabaseChanges()
	{
		return WithPropertyValue(
			builder => builder._allowDatabaseChanges,
			(builder, value) => builder._allowDatabaseChanges = value,
			true);
	}

	internal ClickHouseContextOptionsBuilder<TContext> WithFacadeRegistry(
		ClickHouseFacadeRegistry<TContext> facadeRegistry)
	{
		ExceptionHelpers.ThrowIfNull(facadeRegistry);

		return WithPropertyValue(
			builder => builder._facadeRegistry,
			(builder, value) => builder._facadeRegistry = value,
			facadeRegistry);
	}

	public ClickHouseContextOptionsBuilder<TContext> ForceSession()
	{
		return WithPropertyValue(
			builder => builder._forceSession,
			(builder, value) => builder._forceSession = value,
			true);
	}

	public ClickHouseContextOptionsBuilder<TContext> WithConnectionString(string connectionString)
	{
		if (connectionString.IsNullOrWhiteSpace())
		{
			throw new ArgumentException($"{nameof(connectionString)} is null or whitespace.");
		}

		return WithPropertyValue(
			builder => builder._connectionString,
			(builder, value) => builder._connectionString = value,
			connectionString);
	}

	protected override ClickHouseContextOptions<TContext> BuildCore()
	{
		var connectionString = _connectionString.NotNullOrThrow();

		if (_forceSession.OrElseValue(false))
		{
			connectionString = GetSessionConnectionString(connectionString);
		}

		return new ClickHouseContextOptions<TContext>
		{
			ConnectionString = connectionString,
			FacadeRegistry = _facadeRegistry.NotNullOrThrow(),
			AllowDatabaseChanges = _allowDatabaseChanges.OrElseValue(false),
			HttpClient = _httpClient.OrDefault(),
			HttpClientFactory = _httpClientFactory.OrDefault(),
			HttpClientName = _httpClientName.OrDefault(),
		};
	}

	private static string GetSessionConnectionString(string connectionString)
	{
		var connectionParameters = connectionString.GetConnectionStringParameters();

		var parameterExists = connectionParameters.TryGetValue(UseSessionParameterName.ToLower(), out var value);

		if (!parameterExists)
		{
			return $"{connectionString.TrimEnd(';')};{UseSessionParameterName}=True;";
		}

		if (value == null || !bool.Parse(value))
		{
			return connectionString.Replace(
				$"{UseSessionParameterName}=False",
				$"{UseSessionParameterName}=True",
				StringComparison.InvariantCultureIgnoreCase);
		}

		return connectionString;
	}
}
