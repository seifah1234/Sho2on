class UserModel {
  final int id;
  final String name;
  final int? branchId;

  UserModel({required this.id, required this.name, this.branchId});

  factory UserModel.fromJson(Map j) {
    return UserModel(
      id: j['id'] ?? j['Id'] ?? 0,
      name: j['name'] ?? j['Name'] ?? '',
      branchId: j['branchId'] ?? j['BranchId'],
    );
  }
}
