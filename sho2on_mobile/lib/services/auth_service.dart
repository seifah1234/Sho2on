import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'api_config.dart';


class AuthService {

  Future<Map<String, dynamic>?> login(String id, String password, String deviceId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/auth/login");

    final body = {
      "id": id,
      "password": password,
      "deviceId": deviceId,
    };
    try{
    final res = await http.post(
      url,
      headers: {"Content-Type": "application/json"},
      body: jsonEncode(body),
    );

    if (res.statusCode == 200) {
      return jsonDecode(res.body);
    } else {
      return null;
    }
    }catch(e){
      SnackBar(content: SnackBar(content: Text("Error occurred during login")), );
      print(e);
    }
    return null;

    
  }


  Future<String> register(String id, String password, String deviceId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/auth/register");

    final body = {
      "id": id.trim(),
      "password": password.trim(),
      "deviceId": deviceId,
    };

    final res = await http.post(
      url,
      headers: {"Content-Type": "application/json"},
      body: jsonEncode(body),
    );

    print(res.statusCode);
    print(res.headers);
    print(res.body);


    if (res.statusCode == 200) return "success";
    return res.body;
  }
}
