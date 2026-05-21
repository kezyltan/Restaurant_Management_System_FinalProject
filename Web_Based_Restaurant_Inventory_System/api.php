<?php
require_once __DIR__ . '/config.php';

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    header('Access-Control-Allow-Origin: *');
    header('Access-Control-Allow-Headers: Content-Type, X-Admin-Token');
    header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
    exit;
}

$db = getDb();
$action = $_GET['action'] ?? $_POST['action'] ?? 'public_list';

function readInput() {
    $input = $_POST;
    if (empty($input)) {
        $raw = file_get_contents('php://input');
        $json = json_decode($raw, true);
        if (is_array($json)) {
            $input = $json;
        } else {
            parse_str($raw, $input);
        }
    }
    return $input;
}

function validateInventoryInput(array $input, $isUpdate = false) {
    $required = ['ingredient_name', 'category', 'quantity', 'unit', 'requested_by', 'branch_area', 'priority', 'status'];
    foreach ($required as $field) {
        if (!$isUpdate || array_key_exists($field, $input)) {
            if (!isset($input[$field]) || trim((string)$input[$field]) === '') {
                jsonResponse(['success' => false, 'message' => 'Please complete the required fields.'], 400);
            }
        }
    }
    $allowedStatuses = ['Pending', 'Approved', 'For Purchase', 'Stocked', 'Rejected', 'Cancelled'];
    if (isset($input['status']) && !in_array($input['status'], $allowedStatuses, true)) {
        jsonResponse(['success' => false, 'message' => 'Invalid status value.'], 400);
    }
    $allowedPriorities = ['Low', 'Normal', 'High', 'Urgent'];
    if (isset($input['priority']) && !in_array($input['priority'], $allowedPriorities, true)) {
        jsonResponse(['success' => false, 'message' => 'Invalid priority value.'], 400);
    }
}

try {
    if ($action === 'public_list') {
        $search = cleanText($_GET['search'] ?? '');
        $category = cleanText($_GET['category'] ?? '');
        $status = cleanText($_GET['status'] ?? '');
        $sql = "SELECT id, ingredient_name, category, quantity, unit, branch_area, status, created_at FROM inventory_requests WHERE status IN ('Approved', 'For Purchase', 'Stocked')";
        $params = [];
        if ($search !== '') {
            $sql .= " AND (ingredient_name LIKE :search OR category LIKE :search OR branch_area LIKE :search)";
            $params[':search'] = '%' . $search . '%';
        }
        if ($category !== '') {
            $sql .= " AND category = :category";
            $params[':category'] = $category;
        }
        if ($status !== '') {
            $sql .= " AND status = :status";
            $params[':status'] = $status;
        }
        $sql .= " ORDER BY datetime(created_at) DESC, id DESC";
        $stmt = $db->prepare($sql);
        $stmt->execute($params);
        jsonResponse(['success' => true, 'data' => $stmt->fetchAll(PDO::FETCH_ASSOC)]);
    }

    if ($action === 'admin_list') {
        requireApiAdmin();
        $stmt = $db->query("SELECT * FROM inventory_requests ORDER BY datetime(created_at) DESC, id DESC");
        jsonResponse(['success' => true, 'data' => $stmt->fetchAll(PDO::FETCH_ASSOC)]);
    }

    if ($action === 'summary') {
        requireApiAdmin();
        $rows = $db->query("SELECT status, COUNT(*) AS total FROM inventory_requests GROUP BY status ORDER BY status")->fetchAll(PDO::FETCH_ASSOC);
        jsonResponse(['success' => true, 'data' => $rows]);
    }

    if ($action === 'add') {
        requireApiAdmin();
        $input = readInput();
        validateInventoryInput($input);
        $stmt = $db->prepare("INSERT INTO inventory_requests (ingredient_name, category, quantity, unit, requested_by, branch_area, priority, remarks, status, updated_at) VALUES (:ingredient_name, :category, :quantity, :unit, :requested_by, :branch_area, :priority, :remarks, :status, CURRENT_TIMESTAMP)");
        $stmt->execute([
            ':ingredient_name' => cleanText($input['ingredient_name']),
            ':category' => cleanText($input['category']),
            ':quantity' => cleanText($input['quantity']),
            ':unit' => cleanText($input['unit']),
            ':requested_by' => cleanText($input['requested_by']),
            ':branch_area' => cleanText($input['branch_area']),
            ':priority' => cleanText($input['priority']),
            ':remarks' => cleanText($input['remarks'] ?? ''),
            ':status' => cleanText($input['status'])
        ]);
        jsonResponse(['success' => true, 'message' => 'Record saved.', 'id' => $db->lastInsertId()]);
    }

    if ($action === 'update') {
        requireApiAdmin();
        $input = readInput();
        $id = (int)($input['id'] ?? 0);
        if ($id <= 0) {
            jsonResponse(['success' => false, 'message' => 'Invalid record selected.'], 400);
        }
        validateInventoryInput($input);
        $stmt = $db->prepare("UPDATE inventory_requests SET ingredient_name = :ingredient_name, category = :category, quantity = :quantity, unit = :unit, requested_by = :requested_by, branch_area = :branch_area, priority = :priority, remarks = :remarks, status = :status, updated_at = CURRENT_TIMESTAMP WHERE id = :id");
        $stmt->execute([
            ':ingredient_name' => cleanText($input['ingredient_name']),
            ':category' => cleanText($input['category']),
            ':quantity' => cleanText($input['quantity']),
            ':unit' => cleanText($input['unit']),
            ':requested_by' => cleanText($input['requested_by']),
            ':branch_area' => cleanText($input['branch_area']),
            ':priority' => cleanText($input['priority']),
            ':remarks' => cleanText($input['remarks'] ?? ''),
            ':status' => cleanText($input['status']),
            ':id' => $id
        ]);
        jsonResponse(['success' => true, 'message' => 'Record updated.']);
    }

    if ($action === 'delete') {
        requireApiAdmin();
        $input = readInput();
        $id = (int)($input['id'] ?? 0);
        if ($id <= 0) {
            jsonResponse(['success' => false, 'message' => 'Invalid record selected.'], 400);
        }
        $stmt = $db->prepare("DELETE FROM inventory_requests WHERE id = :id");
        $stmt->execute([':id' => $id]);
        jsonResponse(['success' => true, 'message' => 'Record deleted.']);
    }

    jsonResponse(['success' => false, 'message' => 'Unknown API action.'], 404);
} catch (Exception $e) {
    jsonResponse(['success' => false, 'message' => $e->getMessage()], 500);
}
?>
