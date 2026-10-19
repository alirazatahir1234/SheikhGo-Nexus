import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

/// Owner app — reads SheikhGo Nexus APIs only (no local business DB).
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
    return MaterialApp(
      title: 'Nexus Owner',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0F766E)),
        useMaterial3: true,
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
      throw Exception(jsonDecode(res.body)['message'] ?? 'Login failed');
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
    if (res.statusCode == 401) throw Exception('Unauthorized');
    if (res.statusCode >= 400) {
      throw Exception(jsonDecode(res.body)['message'] ?? 'Request failed');
    }
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<List<dynamic>> getList(String path) async {
    final res = await http.get(Uri.parse('$apiBase$path'), headers: headers);
    if (res.statusCode >= 400) {
      throw Exception(jsonDecode(res.body)['message'] ?? 'Request failed');
    }
    return jsonDecode(res.body) as List<dynamic>;
  }
}

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
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 360),
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Nexus Owner', style: Theme.of(context).textTheme.headlineMedium),
                const SizedBox(height: 8),
                const Text('Daily sales, stock alerts, and profit — live from ERP.'),
                const SizedBox(height: 24),
                TextField(controller: email, decoration: const InputDecoration(labelText: 'Email')),
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
                  onPressed: busy ? null : _submit,
                  child: Text(busy ? 'Signing in…' : 'Sign in'),
                ),
              ],
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

  @override
  void initState() {
    super.initState();
    refresh();
  }

  Future<void> refresh() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final d = await api.getJson('/pos/reports/daily-sales');
      final p = await api.getJson('/pos/reports/owner-profit');
      final products = await api.getList('/products');
      final low = products.where((x) {
        final m = x as Map<String, dynamic>;
        final stock = (m['stockQty'] as num?)?.toDouble() ?? 0;
        final reorder = (m['reorderLevel'] as num?)?.toDouble() ?? 0;
        return stock <= reorder;
      }).toList();
      setState(() {
        daily = d;
        profit = p;
        lowStock = low;
        loading = false;
      });
    } catch (e) {
      setState(() {
        error = e.toString();
        loading = false;
      });
    }
  }

  String money(dynamic v) {
    final n = (v as num?)?.toDouble() ?? 0;
    return 'PKR ${n.toStringAsFixed(0)}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Owner dashboard'),
        actions: [
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
              ? Center(child: Text(error!))
              : RefreshIndicator(
                  onRefresh: refresh,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
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
                          _kpi('Gross profit', money(profit?['grossProfit'])),
                          _kpi('Operating', money(profit?['operatingProfit'])),
                        ],
                      ),
                      const SizedBox(height: 24),
                      Text('Low stock', style: Theme.of(context).textTheme.titleLarge),
                      const SizedBox(height: 8),
                      if (lowStock.isEmpty)
                        const Text('No low-stock items.')
                      else
                        ...lowStock.take(12).map((x) {
                          final m = x as Map<String, dynamic>;
                          return ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: Text('${m['name']}'),
                            subtitle: Text('${m['code']} · ${m['unit']}'),
                            trailing: Text('${m['stockQty']}'),
                          );
                        }),
                    ],
                  ),
                ),
    );
  }

  Widget _kpi(String label, String value) {
    return SizedBox(
      width: 150,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(12),
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
