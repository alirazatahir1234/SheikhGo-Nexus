import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

/// Owner app — read-only reporting from SheikhGo Nexus APIs (no cashier POS).
/// Pass API base at build/run: `--dart-define=API_BASE=http://10.0.2.2:5080/api`
const apiBase = String.fromEnvironment(
  'API_BASE',
  defaultValue: 'http://localhost:5080/api',
);

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const NexusOwnerApp());
}

class NexusOwnerApp extends StatelessWidget {
  const NexusOwnerApp({super.key});

  @override
  Widget build(BuildContext context) {
    final scheme = ColorScheme.fromSeed(
      seedColor: const Color(0xFF0F766E),
      brightness: Brightness.light,
    );
    return MaterialApp(
      title: 'Nexus Owner',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: scheme,
        useMaterial3: true,
        scaffoldBackgroundColor: const Color(0xFFF4F7F8),
        cardTheme: CardThemeData(
          elevation: 0,
          color: Colors.white,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
            side: BorderSide(color: Colors.black.withValues(alpha: 0.06)),
          ),
        ),
        inputDecorationTheme: const InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(),
        ),
      ),
      home: const BootPage(),
    );
  }
}

class ApiClient {
  String? token;

  Map<String, String> get headers => {
        'Content-Type': 'application/json',
        if (token != null) 'Authorization': 'Bearer $token',
      };

  Future<Map<String, dynamic>> login(String email, String password) async {
    final res = await http.post(
      Uri.parse('$apiBase/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'email': email, 'password': password}),
    );
    if (res.statusCode >= 400) {
      throw Exception(_message(res) ?? 'Login failed');
    }
    final body = jsonDecode(res.body) as Map<String, dynamic>;
    token = body['token'] as String?;
    if (token == null) throw Exception('No token in login response');
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', token!);
    return body;
  }

  Future<void> loadToken() async {
    final prefs = await SharedPreferences.getInstance();
    token = prefs.getString('token');
  }

  Future<void> logout() async {
    token = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
  }

  Future<Map<String, dynamic>> getJson(String path) async {
    final res = await http.get(Uri.parse('$apiBase$path'), headers: headers);
    if (res.statusCode == 401) throw UnauthorizedException();
    if (res.statusCode >= 400) {
      throw Exception(_message(res) ?? 'Request failed');
    }
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<List<dynamic>> getList(String path) async {
    final res = await http.get(Uri.parse('$apiBase$path'), headers: headers);
    if (res.statusCode == 401) throw UnauthorizedException();
    if (res.statusCode >= 400) {
      throw Exception(_message(res) ?? 'Request failed');
    }
    return jsonDecode(res.body) as List<dynamic>;
  }

  String? _message(http.Response res) {
    try {
      final body = jsonDecode(res.body);
      if (body is Map && body['message'] != null) return body['message'].toString();
    } catch (_) {}
    return null;
  }
}

class UnauthorizedException implements Exception {}

final api = ApiClient();

class BootPage extends StatefulWidget {
  const BootPage({super.key});
  @override
  State<BootPage> createState() => _BootPageState();
}

class _BootPageState extends State<BootPage> {
  @override
  void initState() {
    super.initState();
    _boot();
  }

  Future<void> _boot() async {
    await api.loadToken();
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => api.token == null ? const LoginPage() : const OwnerHomePage(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) =>
      const Scaffold(body: Center(child: CircularProgressIndicator()));
}

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final email = TextEditingController(text: 'admin@softaxe.local');
  final password = TextEditingController(text: 'Admin@123');
  String? error;
  bool busy = false;

  Future<void> _submit() async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await api.login(email.text.trim(), password.text);
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const OwnerHomePage()),
      );
    } catch (e) {
      setState(() => error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 400),
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const SizedBox(height: 24),
                  Text(
                    'Nexus Owner',
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.w700,
                          color: const Color(0xFF0F766E),
                        ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Today’s sales, profit, and low stock — live from ERP.',
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: Colors.black54),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'API: $apiBase',
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black38),
                  ),
                  const SizedBox(height: 28),
                  TextField(
                    controller: email,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(labelText: 'Email'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: password,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: 'Password'),
                  ),
                  if (error != null) ...[
                    const SizedBox(height: 12),
                    Text(error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                  ],
                  const SizedBox(height: 20),
                  FilledButton(
                    style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
                    onPressed: busy ? null : _submit,
                    child: Text(busy ? 'Signing in…' : 'Sign in'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class OwnerHomePage extends StatefulWidget {
  const OwnerHomePage({super.key});
  @override
  State<OwnerHomePage> createState() => _OwnerHomePageState();
}

class _OwnerHomePageState extends State<OwnerHomePage> {
  Map<String, dynamic>? daily;
  Map<String, dynamic>? profit;
  List<dynamic> lowStock = [];
  String? error;
  bool loading = true;
  late String reportDate;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    reportDate =
        '${now.year.toString().padLeft(4, '0')}-${now.month.toString().padLeft(2, '0')}-${now.day.toString().padLeft(2, '0')}';
    refresh();
  }

  Future<void> refresh() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final d = await api.getJson('/pos/reports/daily-sales?date=$reportDate');
      final p = await api.getJson('/pos/reports/owner-profit?date=$reportDate');
      final products = await api.getList('/products');
      final low = products.where((x) {
        final m = x as Map<String, dynamic>;
        final stock = (m['stockQty'] as num?)?.toDouble() ?? 0;
        final reorder = (m['reorderLevel'] as num?)?.toDouble() ?? 0;
        return reorder > 0 && stock <= reorder;
      }).toList();
      low.sort((a, b) {
        final sa = ((a as Map)['stockQty'] as num?)?.toDouble() ?? 0;
        final sb = ((b as Map)['stockQty'] as num?)?.toDouble() ?? 0;
        return sa.compareTo(sb);
      });
      setState(() {
        daily = d;
        profit = p;
        lowStock = low;
        loading = false;
      });
    } on UnauthorizedException {
      await api.logout();
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const LoginPage()),
      );
    } catch (e) {
      setState(() {
        error = e.toString().replaceFirst('Exception: ', '');
        loading = false;
      });
    }
  }

  String money(dynamic v) {
    final n = (v as num?)?.toDouble() ?? 0;
    return 'PKR ${n.toStringAsFixed(0)}';
  }

  Future<void> pickDate() async {
    final initial = DateTime.tryParse(reportDate) ?? DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 1)),
    );
    if (picked == null) return;
    setState(() {
      reportDate =
          '${picked.year.toString().padLeft(4, '0')}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}';
    });
    await refresh();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Owner dashboard'),
        actions: [
          IconButton(onPressed: pickDate, icon: const Icon(Icons.calendar_today), tooltip: 'Report date'),
          IconButton(onPressed: refresh, icon: const Icon(Icons.refresh)),
          IconButton(
            onPressed: () async {
              await api.logout();
              if (!context.mounted) return;
              Navigator.of(context).pushReplacement(
                MaterialPageRoute(builder: (_) => const LoginPage()),
              );
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(error!, textAlign: TextAlign.center),
                        const SizedBox(height: 16),
                        FilledButton(onPressed: refresh, child: const Text('Retry')),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: refresh,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      Text(
                        'Report · $reportDate',
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(color: Colors.black54),
                      ),
                      const SizedBox(height: 12),
                      Text("Today's sales", style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 12),
                      Wrap(
                        spacing: 12,
                        runSpacing: 12,
                        children: [
                          _kpi('Net sales', money(daily?['netSales'])),
                          _kpi('Tickets', '${daily?['saleCount'] ?? 0}'),
                          _kpi('Cash', money(daily?['cashTotal'])),
                          _kpi('Card', money(daily?['cardTotal'])),
                        ],
                      ),
                      const SizedBox(height: 24),
                      Text('Owner profit', style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 12),
                      Wrap(
                        spacing: 12,
                        runSpacing: 12,
                        children: [
                          _kpi('Gross profit', money(profit?['grossProfit'])),
                          _kpi('Operating', money(profit?['operatingProfit'])),
                          _kpi('COGS', money(profit?['cogs'])),
                          _kpi('Expenses', money(profit?['expenses'])),
                        ],
                      ),
                      const SizedBox(height: 24),
                      Text('Low stock', style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 4),
                      Text(
                        'Items at or below reorder level (same product stock as ERP).',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.black54),
                      ),
                      const SizedBox(height: 8),
                      if (lowStock.isEmpty)
                        const Card(
                          child: ListTile(title: Text('No low-stock items.')),
                        )
                      else
                        ...lowStock.take(20).map((x) {
                          final m = x as Map<String, dynamic>;
                          final stock = m['stockQty'];
                          final reorder = m['reorderLevel'];
                          return Card(
                            margin: const EdgeInsets.only(bottom: 8),
                            child: ListTile(
                              title: Text('${m['name']}'),
                              subtitle: Text('${m['code']} · reorder $reorder'),
                              trailing: Text(
                                '$stock',
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                  color: Color(0xFFB45309),
                                ),
                              ),
                            ),
                          );
                        }),
                    ],
                  ),
                ),
    );
  }

  Widget _kpi(String label, String value) {
    return SizedBox(
      width: 156,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: const TextStyle(fontSize: 12, color: Colors.black54)),
              const SizedBox(height: 6),
              Text(value, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
            ],
          ),
        ),
      ),
    );
  }
}
