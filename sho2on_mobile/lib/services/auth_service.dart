import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'api_config.dart';


class AuthService {

  Future<Map<String, Map<String, dynamic>?>> login(String username, String password, String deviceId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/auth/login");

    final body = {
      "username": username,
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
      return {"200": jsonDecode(res.body)};
    } else if (res.statusCode == 500) {
      return {"500": null};
    }
    else {
      return {res.body: null};
    }
    }catch(e){
      SnackBar(content: SnackBar(content: Text("Error occurred during login")), );
      print(e);
    }
    return {"Error": null};
  }


  Future<String> register(String id, String username, String password, String deviceId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/auth/register");

    final body = {
      "id": id.trim(),
      "username": username.trim(),
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
