using System;
using System.Collections;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using roundhouse.databases;
using roundhouse.databases.mysql;
using roundhouse.infrastructure.app;
using roundhouse.infrastructure.logging;

namespace RoundhousePercona
{
    public sealed class MySqlDatabaseWithOnlineSchemaChange : Database
    {
        private readonly Database database = new MySqlDatabase();

        public ConfigurationPropertyHolder configuration
        {
            get { return database.configuration; }
            set
            {
                //Dirty hack for MySqlDatabase work. Hardcoded databases list in NHibernateSessionFactoryBuilder
                value.DatabaseType = "roundhouse.databases.mysql.MySqlDatabase, roundhouse.databases.mysql";
                database.configuration = value;
            }
        }

        public void run_sql(string sql_to_run, ConnectionType connection_type)
        {
            var words = to_words(sql_to_run);
            if (is_alter_table(words))
            {
                alter_table(sql_to_run, words, connection_type == ConnectionType.Admin ? admin_connection_string : connection_string);
                return;
            }

            database.run_sql(sql_to_run, connection_type);
        }

        #region Other interafce implemntation methods

        public string connection_string
        {
            get { return database.connection_string; }
            set { database.connection_string = value; }
        }

        public string admin_connection_string
        {
            get { return database.admin_connection_string; }
            set { database.admin_connection_string = value; }
        }

        public string server_name
        {
            get { return database.server_name; }
            set { database.server_name = value; }
        }

        public string database_name
        {
            get { return database.database_name; }
            set { database.database_name = value; }
        }

        public string provider
        {
            get { return database.provider; }
            set { database.provider = value; }
        }

        public string roundhouse_schema_name
        {
            get { return database.roundhouse_schema_name; }
            set { database.roundhouse_schema_name = value; }
        }

        public string version_table_name
        {
            get { return database.version_table_name; }
            set { database.version_table_name = value; }
        }

        public string scripts_run_table_name
        {
            get { return database.scripts_run_table_name; }
            set { database.scripts_run_table_name = value; }
        }

        public string scripts_run_errors_table_name
        {
            get { return database.scripts_run_errors_table_name; }
            set { database.scripts_run_errors_table_name = value; }
        }

        public string user_name
        {
            get { return database.user_name; }
            set { database.user_name = value; }
        }

        public string sql_statement_separator_regex_pattern
        {
            get { return database.sql_statement_separator_regex_pattern; }
        }

        public int command_timeout
        {
            get { return database.command_timeout; }
            set { database.command_timeout = value; }
        }

        public int admin_command_timeout
        {
            get { return database.admin_command_timeout; }
            set { database.admin_command_timeout = value; }
        }

        public int restore_timeout
        {
            get { return database.restore_timeout; }
            set { database.restore_timeout = value; }
        }

        public bool split_batch_statements
        {
            get { return database.split_batch_statements; }
            set { database.split_batch_statements = value; }
        }

        public bool supports_ddl_transactions
        {
            get { return database.supports_ddl_transactions; }
        }

        public void initialize_connections(ConfigurationPropertyHolder configuration_property_holder)
        {
            database.initialize_connections(configuration_property_holder);
        }

        public void open_admin_connection()
        {
            database.open_admin_connection();
        }

        public void close_admin_connection()
        {
            database.close_admin_connection();
        }
        public void open_connection(bool with_transaction)
        {
            database.open_connection(with_transaction);
        }

        public void close_connection()
        {
            database.close_connection();
        }

        public void rollback()
        {
            database.rollback();
        }

        public bool create_database_if_it_doesnt_exist(string custom_create_database_script)
        {
            return database.create_database_if_it_doesnt_exist(custom_create_database_script);
        }

        public void set_recovery_mode(bool simple)
        {
            database.set_recovery_mode(simple);
        }

        public void backup_database(string output_path_minus_database)
        {
            database.backup_database(output_path_minus_database);
        }

        public void restore_database(string restore_from_path, string custom_restore_options)
        {
            database.restore_database(restore_from_path, custom_restore_options);
        }

        public void delete_database_if_it_exists()
        {
            database.delete_database_if_it_exists();
        }

        public void run_database_specific_tasks()
        {
            database.run_database_specific_tasks();
        }

        public void create_or_update_roundhouse_tables()
        {
            database.create_or_update_roundhouse_tables();
        }

        public object run_sql_scalar(string sql_to_run, ConnectionType connection_type)
        {
            return database.run_sql_scalar(sql_to_run, connection_type);
        }

        public void insert_script_run(string script_name, string sql_to_run, string sql_to_run_hash, bool run_this_script_once, long version_id)
        {
            database.insert_script_run(script_name, sql_to_run, sql_to_run_hash, run_this_script_once, version_id);
        }

        public void insert_script_run_error(string script_name, string sql_to_run, string sql_erroneous_part, string error_message, string repository_version, string repository_path)
        {
            database.insert_script_run_error(script_name, sql_to_run, sql_erroneous_part, error_message, repository_version, repository_path);
        }

        public string get_version(string repository_path)
        {
            return database.get_version(repository_path);
        }

        public long insert_version_and_get_version_id(string repository_path, string repository_version)
        {
            return database.insert_version_and_get_version_id(repository_path, repository_version);
        }

        public bool has_run_script_already(string script_name)
        {
            return database.has_run_script_already(script_name);
        }

        public string get_current_script_hash(string script_name)
        {
            return database.get_current_script_hash(script_name);
        }

        #endregion

        private bool disposing = false;
        public void Dispose()
        {
            if (!disposing)
            {
                database.Dispose();
                disposing = true;
            }
        }

        private static bool is_alter_table(string[] words)
        {
            var alter_position = Array.FindIndex(words, t => t.Equals("alter", StringComparison.OrdinalIgnoreCase));
            if (alter_position == -1)
            {
                return false;
            }

            return words[alter_position + 1].Equals("table", StringComparison.OrdinalIgnoreCase);
        }

        private void alter_table(string sql_to_run, string[] words, string connection_string)
        {
            if (!sql_to_run.StartsWith("ALTER TABLE"))
            {
                throw new ArgumentException("alter table statement must be starts with 'ALTER TABLE'");
            }

            var table_name = words[2];
            var alter_sql = sql_to_run.Substring(sql_to_run.IndexOf(words[3])).Replace(Environment.NewLine, " ");
            var toolArguments = new StringBuilder()
                .Append(" --execute")
                .Append(" --alter \"" + alter_sql + "\"")
                .Append(" " + get_command_line_argument("pt-online-schema-change-options", false));

            add_datasource_arguments(toolArguments, table_name, connection_string);

            Log.bound_to(this).log_an_info_event_containing("run pt-online-schema-change with arguments:" + toolArguments);

            var result = BatchFileExecutor.execute_bat_file(
                filePath: get_command_line_argument("pt-online-schema-change-path", true),
                arguments: toolArguments.ToString(),
                onOutput: m => Log.bound_to(this).log_an_info_event_containing(m),
                onError: m => Log.bound_to(this).log_an_error_event_containing(m)
             );

            if (result != 0)
            {
                throw new Exception("ALTER TABLE processing failed");
            }
        }

        private string get_command_line_argument(string argumentName, bool required)
        {
            var prefix = "--" + argumentName + "=";

            var value = Environment.GetCommandLineArgs()
                .Where(a => a.StartsWith(prefix))
                .Select(a => a.Substring(prefix.Length).Trim('\"'))
                .FirstOrDefault();
            
            if (required && string.IsNullOrEmpty(value)) {
                throw new InvalidOperationException("Argument " + argumentName + " required");
            }

            return value;
        }

        private void add_command_line_arguments(StringBuilder argumentsBuilder)
        {
            foreach (var argument in Environment.GetCommandLineArgs()) {
                const string argumentPrefix = "--pct-";
                if (argument.StartsWith(argumentPrefix)) {
                    argumentsBuilder.Append(" --").Append(argument, argumentPrefix.Length, argument.Length - argumentPrefix.Length);
                }
            }
        }

        private void add_datasource_arguments(StringBuilder argumentsBuilder, string table_name, string connection_string)
        {
            var connection_string_parameters = parse_connection_string(connection_string);

            argumentsBuilder.Append(" h=" + connection_string_parameters["server"])
                .Append(",P=" + (connection_string_parameters.Contains("port") ? connection_string_parameters["port"] : 3306))
                .Append(",u=" + connection_string_parameters["uid"])
                .Append(",p=" + connection_string_parameters["pwd"])
                .Append(",D=" + connection_string_parameters["database"])
                .Append(",t=" + table_name);
        }

        private static string[] to_words(string input)
        {
            return Regex.Matches(input, @"[\w\d_]+", RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m => m.Value)
                .ToArray();
        }

        private IDictionary parse_connection_string(string connectionString)
        {
            var connectionBuilder = new DbConnectionStringBuilder();
            connectionBuilder.ConnectionString = configuration.ConnectionString;

            return connectionBuilder;
        }
    }
}
