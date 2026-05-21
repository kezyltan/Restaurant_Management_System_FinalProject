<?php
// Database configuration and helper functions
// Connects to SQLite via PDO
// Migration logic for inventory_requests table
// Seed initial inventory records for testing
const ADMIN_USERNAME = 'admin';
const ADMIN_PASSWORD = 'admin123';
const API_ADMIN_TOKEN = 'restaurant-admin-token-2026';

function getDb() {
    $dbFile = __DIR__ . DIRECTORY_SEPARATOR . 'restaurant_inventory.sqlite';
    try {
        $db = new PDO('sqlite:' . $dbFile);
    } catch (Exception $e) {
        http_response_code(500);
        echo 'SQLite is not enabled. Please enable pdo_sqlite in your PHP setup.';
        exit;
    }
    $db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    $db->exec("CREATE TABLE IF NOT EXISTS inventory_requests (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        ingredient_name TEXT NOT NULL,
        category TEXT NOT NULL,
        quantity TEXT NOT NULL,
        unit TEXT NOT NULL,
        requested_by TEXT NOT NULL,
        branch_area TEXT NOT NULL,
        priority TEXT NOT NULL DEFAULT 'Normal',
        remarks TEXT,
        status TEXT NOT NULL DEFAULT 'Pending',
        created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
        updated_at TEXT
    )");
    seedData($db);
    return $db;
}

function seedData(PDO $db) {
    $count = (int)$db->query("SELECT COUNT(*) FROM inventory_requests")->fetchColumn();
    if ($count > 0) {
        return;
    }
    $stmt = $db->prepare("INSERT INTO inventory_requests (ingredient_name, category, quantity, unit, requested_by, branch_area, priority, remarks, status) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
    $rows = [
        ['Chicken Breast', 'Meat', '15', 'kg', 'Kitchen Staff', 'Main Kitchen', 'High', 'Good for grilled meals and rice bowls.', 'Pending'],
        ['Tomato Sauce', 'Condiments', '12', 'bottles', 'Chef Marco', 'Sauce Station', 'Normal', 'Used for pasta and pizza orders.', 'Approved'],
        ['Lettuce', 'Vegetables', '8', 'kg', 'Salad Prep', 'Cold Station', 'Urgent', 'Low stock before dinner service.', 'For Purchase'],
        ['Takeout Boxes', 'Packaging', '300', 'pcs', 'Front Counter', 'Service Area', 'Normal', 'For takeout and delivery orders.', 'Stocked'],
        ['Fresh Shrimp', 'Seafood', '10', 'kg', 'Kitchen Staff', 'Seafood Station', 'High', 'Needed for weekend menu.', 'Approved']
    ];
    foreach ($rows as $row) {
        $stmt->execute($row);
    }
}

function startSessionSafe() {
    if (session_status() !== PHP_SESSION_ACTIVE) {
        session_start();
    }
}

function isAdminLoggedIn() {
    startSessionSafe();
    return !empty($_SESSION['admin_logged_in']);
}

function requireAdminPage() {
    if (!isAdminLoggedIn()) {
        header('Location: index.php');
        exit;
    }
}

function csrfToken() {
    startSessionSafe();
    if (empty($_SESSION['csrf_token'])) {
        $_SESSION['csrf_token'] = bin2hex(random_bytes(24));
    }
    return $_SESSION['csrf_token'];
}

function verifyCsrf() {
    startSessionSafe();
    $sent = $_POST['csrf_token'] ?? '';
    if (!$sent || empty($_SESSION['csrf_token']) || !hash_equals($_SESSION['csrf_token'], $sent)) {
        http_response_code(403);
        exit('Invalid request token.');
    }
}

function requireApiAdmin() {
    $headers = function_exists('getallheaders') ? getallheaders() : [];
    $token = $_GET['token'] ?? $_POST['token'] ?? '';
    foreach ($headers as $key => $value) {
        if (strtolower($key) === 'x-admin-token') {
            $token = $value;
            break;
        }
    }
    if (!hash_equals(API_ADMIN_TOKEN, (string)$token)) {
        jsonResponse(['success' => false, 'message' => 'Unauthorized request.'], 401);
    }
}

function cleanText($value) {
    return trim((string)$value);
}

function jsonResponse($data, $code = 200) {
    http_response_code($code);
    header('Content-Type: application/json; charset=utf-8');
    header('Access-Control-Allow-Origin: *');
    header('Access-Control-Allow-Headers: Content-Type, X-Admin-Token');
    echo json_encode($data, JSON_PRETTY_PRINT);
    exit;
}
?>
