<?php
require_once __DIR__ . '/config.php';
startSessionSafe();
$db = getDb();
$notice = '';
$error = '';

$categories = ['Meat', 'Vegetables', 'Seafood', 'Condiments', 'Beverages', 'Packaging', 'Cleaning Supply', 'Other'];
$priorities = ['Low', 'Normal', 'High', 'Urgent'];
$statuses = ['Pending', 'Approved', 'For Purchase', 'Stocked', 'Rejected', 'Cancelled'];

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $formAction = $_POST['form_action'] ?? '';

    if ($formAction === 'login') {
        $username = trim($_POST['username'] ?? '');
        $password = trim($_POST['password'] ?? '');
        if (hash_equals(ADMIN_USERNAME, $username) && hash_equals(ADMIN_PASSWORD, $password)) {
            session_regenerate_id(true);
            $_SESSION['admin_logged_in'] = true;
            $_SESSION['admin_name'] = $username;
            header('Location: index.php');
            exit;
        }
        $error = 'Invalid username or password.';
    }

    if ($formAction === 'logout') {
        verifyCsrf();
        session_destroy();
        header('Location: index.php');
        exit;
    }

    if (isAdminLoggedIn() && in_array($formAction, ['create', 'update', 'delete'], true)) {
        verifyCsrf();
        try {
            if ($formAction === 'delete') {
                $id = (int)($_POST['id'] ?? 0);
                if ($id <= 0) {
                    throw new Exception('Select a valid record to delete.');
                }
                $stmt = $db->prepare('DELETE FROM inventory_requests WHERE id = ?');
                $stmt->execute([$id]);
                header('Location: index.php?notice=deleted');
                exit;
            }

            $data = [
                'ingredient_name' => cleanText($_POST['ingredient_name'] ?? ''),
                'category' => cleanText($_POST['category'] ?? ''),
                'quantity' => cleanText($_POST['quantity'] ?? ''),
                'unit' => cleanText($_POST['unit'] ?? ''),
                'requested_by' => cleanText($_POST['requested_by'] ?? ''),
                'branch_area' => cleanText($_POST['branch_area'] ?? ''),
                'priority' => cleanText($_POST['priority'] ?? 'Normal'),
                'remarks' => cleanText($_POST['remarks'] ?? ''),
                'status' => cleanText($_POST['status'] ?? 'Pending')
            ];

            foreach (['ingredient_name', 'category', 'quantity', 'unit', 'requested_by', 'branch_area', 'priority', 'status'] as $field) {
                if ($data[$field] === '') {
                    throw new Exception('Please complete the required fields.');
                }
            }
            if (!in_array($data['category'], $categories, true)) {
                throw new Exception('Invalid category selected.');
            }
            if (!in_array($data['priority'], $priorities, true)) {
                throw new Exception('Invalid priority selected.');
            }
            if (!in_array($data['status'], $statuses, true)) {
                throw new Exception('Invalid status selected.');
            }

            if ($formAction === 'create') {
                $stmt = $db->prepare('INSERT INTO inventory_requests (ingredient_name, category, quantity, unit, requested_by, branch_area, priority, remarks, status, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)');
                $stmt->execute(array_values($data));
                header('Location: index.php?notice=created');
                exit;
            }

            if ($formAction === 'update') {
                $id = (int)($_POST['id'] ?? 0);
                if ($id <= 0) {
                    throw new Exception('Select a valid record to update.');
                }
                $stmt = $db->prepare('UPDATE inventory_requests SET ingredient_name = ?, category = ?, quantity = ?, unit = ?, requested_by = ?, branch_area = ?, priority = ?, remarks = ?, status = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?');
                $values = array_values($data);
                $values[] = $id;
                $stmt->execute($values);
                header('Location: index.php?notice=updated');
                exit;
            }
        } catch (Exception $ex) {
            $error = $ex->getMessage();
        }
    }
}

if (!empty($_GET['notice'])) {
    $messages = [
        'created' => 'Inventory record added.',
        'updated' => 'Inventory record updated.',
        'deleted' => 'Inventory record deleted.'
    ];
    $notice = $messages[$_GET['notice']] ?? '';
}

function e($value) {
    return htmlspecialchars((string)$value, ENT_QUOTES, 'UTF-8');
}

function selected($actual, $expected) {
    return (string)$actual === (string)$expected ? 'selected' : '';
}

function statusClass($status) {
    return 'status-' . strtolower(preg_replace('/[^a-z0-9]+/i', '-', (string)$status));
}

$editing = null;
if (isAdminLoggedIn() && isset($_GET['edit'])) {
    $stmt = $db->prepare('SELECT * FROM inventory_requests WHERE id = ?');
    $stmt->execute([(int)$_GET['edit']]);
    $editing = $stmt->fetch(PDO::FETCH_ASSOC) ?: null;
}

$search = cleanText($_GET['search'] ?? '');
$filterStatus = cleanText($_GET['status'] ?? '');
$filterCategory = cleanText($_GET['category'] ?? '');
$params = [];
$sql = 'SELECT * FROM inventory_requests WHERE 1 = 1';
if ($search !== '') {
    $sql .= ' AND (ingredient_name LIKE :search OR requested_by LIKE :search OR branch_area LIKE :search OR remarks LIKE :search)';
    $params[':search'] = '%' . $search . '%';
}
if ($filterStatus !== '') {
    $sql .= ' AND status = :status';
    $params[':status'] = $filterStatus;
}
if ($filterCategory !== '') {
    $sql .= ' AND category = :category';
    $params[':category'] = $filterCategory;
}
$sql .= ' ORDER BY datetime(created_at) DESC, id DESC';
$stmt = $db->prepare($sql);
$stmt->execute($params);
$records = $stmt->fetchAll(PDO::FETCH_ASSOC);

$totalRecords = (int)$db->query('SELECT COUNT(*) FROM inventory_requests')->fetchColumn();
$pendingRecords = (int)$db->query("SELECT COUNT(*) FROM inventory_requests WHERE status = 'Pending'")->fetchColumn();
$forPurchaseRecords = (int)$db->query("SELECT COUNT(*) FROM inventory_requests WHERE status = 'For Purchase'")->fetchColumn();
$stockedRecords = (int)$db->query("SELECT COUNT(*) FROM inventory_requests WHERE status = 'Stocked'")->fetchColumn();
?>
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Restaurant Inventory Admin</title>
<style>
:root{--ink:#1c1914;--cream:#fff8ec;--panel:rgba(255,248,236,.92);--green:#284734;--sage:#6f8d65;--gold:#c99135;--rust:#9a4c2f;--line:#eadcc9}
*{box-sizing:border-box}body{margin:0;min-height:100vh;font-family:Segoe UI,Arial,sans-serif;color:var(--ink);background:linear-gradient(135deg,rgba(18,27,20,.88),rgba(52,33,21,.78)),url('assets/restaurant_inventory_bg.png') center/cover fixed no-repeat}.login-wrap{min-height:100vh;display:grid;place-items:center;padding:24px}.login-card{width:min(440px,100%);background:var(--panel);border:1px solid rgba(255,255,255,.5);border-radius:28px;box-shadow:0 30px 80px rgba(0,0,0,.34);padding:34px}.brand{display:flex;align-items:center;gap:14px;margin-bottom:18px}.brand-mark{width:54px;height:54px;border-radius:18px;background:linear-gradient(135deg,var(--green),var(--gold));display:grid;place-items:center;color:white;font-weight:900;font-size:24px;box-shadow:0 10px 25px rgba(40,71,52,.34)}h1,h2,h3,p{margin-top:0}.muted{color:#675d50}.login-card h1{font-size:30px;margin-bottom:6px}label{display:block;font-weight:700;margin:12px 0 7px}input,select,textarea{width:100%;border:1px solid var(--line);border-radius:13px;padding:12px 13px;font-size:14px;background:white;color:var(--ink);outline:none}textarea{resize:vertical}.btn{border:0;border-radius:13px;padding:12px 16px;font-weight:800;cursor:pointer;text-decoration:none;display:inline-flex;justify-content:center;align-items:center;gap:6px}.btn-primary{background:linear-gradient(135deg,var(--green),#446f4d);color:white;box-shadow:0 10px 25px rgba(40,71,52,.25)}.btn-gold{background:var(--gold);color:#2d1d0d}.btn-light{background:#fff;color:var(--ink);border:1px solid var(--line)}.btn-danger{background:#b64e39;color:white}.alert{padding:12px 14px;border-radius:14px;margin:12px 0;font-weight:700}.alert-ok{background:#e4f3d8;color:#294922}.alert-error{background:#ffe4db;color:#82331f}.app{max-width:1440px;margin:auto;padding:24px}.topbar{display:flex;align-items:center;justify-content:space-between;gap:18px;margin-bottom:20px}.titlebox{background:rgba(255,248,236,.12);border:1px solid rgba(255,255,255,.25);border-radius:24px;padding:20px 24px;color:white;backdrop-filter:blur(12px);flex:1}.titlebox h1{font-size:34px;margin-bottom:4px}.top-actions{display:flex;gap:10px;align-items:center}.stats{display:grid;grid-template-columns:repeat(4,1fr);gap:14px;margin-bottom:18px}.stat{background:var(--panel);border:1px solid rgba(255,255,255,.45);border-radius:22px;padding:18px;box-shadow:0 18px 50px rgba(0,0,0,.18)}.stat strong{font-size:30px;display:block;color:var(--green)}.layout{display:grid;grid-template-columns:370px minmax(0,1fr);gap:18px}.card{background:var(--panel);border:1px solid rgba(255,255,255,.5);border-radius:24px;box-shadow:0 22px 60px rgba(0,0,0,.18);padding:22px}.form-row{display:grid;grid-template-columns:1fr 1fr;gap:10px}.form-actions{display:flex;gap:10px;flex-wrap:wrap;margin-top:16px}.filters{display:grid;grid-template-columns:1.5fr 1fr 1fr auto;gap:10px;margin-bottom:14px}.table-wrap{overflow:auto;border-radius:17px;border:1px solid var(--line);background:white}table{width:100%;border-collapse:collapse;min-width:1000px}th,td{padding:11px 12px;border-bottom:1px solid #f0e5d7;text-align:left;font-size:14px;vertical-align:top}th{position:sticky;top:0;background:#2f4b35;color:white;z-index:1}tr:hover td{background:#fff6e8}.badge{display:inline-block;border-radius:999px;padding:5px 9px;font-weight:800;font-size:12px;background:#eef4e8;color:#2f4b35}.badge.status-pending{background:#fff0c7;color:#7a4b00}.badge.status-approved{background:#ddf2dc;color:#1f642a}.badge.status-for-purchase{background:#e4efff;color:#244f87}.badge.status-stocked{background:#dff7ef;color:#17624d}.badge.status-rejected,.badge.status-cancelled{background:#ffe2db;color:#853725}.row-actions{display:flex;gap:8px;flex-wrap:wrap}.inline-form{display:inline}.small{font-size:12px;color:#74695c}.empty{padding:30px;text-align:center;color:#756b5f}@media(max-width:1000px){.layout,.stats{grid-template-columns:1fr}.filters{grid-template-columns:1fr}.topbar{flex-direction:column;align-items:stretch}}
</style>
</head>
<body>
<?php if (!isAdminLoggedIn()): ?>
<div class="login-wrap">
  <form class="login-card" method="post">
    <input type="hidden" name="form_action" value="login">
    <div class="brand"><div class="brand-mark">RI</div><div><h1>Admin Console</h1><p class="muted">Restaurant inventory management</p></div></div>
    <?php if ($error): ?><div class="alert alert-error"><?=e($error)?></div><?php endif; ?>
    <label>Username</label>
    <input name="username" autocomplete="username" required autofocus>
    <label>Password</label>
    <input name="password" type="password" autocomplete="current-password" required>
    <button class="btn btn-primary" style="width:100%;margin-top:18px" type="submit">Sign In</button>
  </form>
</div>
<?php else: ?>
<div class="app">
  <div class="topbar">
    <div class="titlebox"><h1>Restaurant Inventory Admin</h1><p>Manage kitchen supplies, request status, stock movement, and purchasing updates.</p></div>
    <div class="top-actions">
      <a class="btn btn-light" href="api.php?action=public_list" target="_blank">Public Data</a>
      <form method="post"><input type="hidden" name="csrf_token" value="<?=e(csrfToken())?>"><input type="hidden" name="form_action" value="logout"><button class="btn btn-gold" type="submit">Sign Out</button></form>
    </div>
  </div>

  <?php if ($notice): ?><div class="alert alert-ok"><?=e($notice)?></div><?php endif; ?>
  <?php if ($error): ?><div class="alert alert-error"><?=e($error)?></div><?php endif; ?>

  <div class="stats">
    <div class="stat"><span>Total Records</span><strong><?=e($totalRecords)?></strong></div>
    <div class="stat"><span>Pending</span><strong><?=e($pendingRecords)?></strong></div>
    <div class="stat"><span>For Purchase</span><strong><?=e($forPurchaseRecords)?></strong></div>
    <div class="stat"><span>Stocked</span><strong><?=e($stockedRecords)?></strong></div>
  </div>

  <div class="layout">
    <div class="card">
      <h2><?= $editing ? 'Update Record' : 'New Inventory Record' ?></h2>
      <p class="muted">Keep stock requests updated before they appear in the customer viewer.</p>
      <form method="post">
        <input type="hidden" name="csrf_token" value="<?=e(csrfToken())?>">
        <input type="hidden" name="form_action" value="<?= $editing ? 'update' : 'create' ?>">
        <?php if ($editing): ?><input type="hidden" name="id" value="<?=e($editing['id'])?>"><?php endif; ?>
        <label>Item Name</label>
        <input name="ingredient_name" required value="<?=e($editing['ingredient_name'] ?? '')?>" placeholder="Chicken Breast">
        <div class="form-row">
          <div><label>Category</label><select name="category" required><?php foreach($categories as $cat): ?><option <?=selected($editing['category'] ?? '', $cat)?>><?=e($cat)?></option><?php endforeach; ?></select></div>
          <div><label>Priority</label><select name="priority" required><?php foreach($priorities as $prio): ?><option <?=selected($editing['priority'] ?? 'Normal', $prio)?>><?=e($prio)?></option><?php endforeach; ?></select></div>
        </div>
        <div class="form-row">
          <div><label>Quantity</label><input name="quantity" required value="<?=e($editing['quantity'] ?? '')?>" placeholder="10"></div>
          <div><label>Unit</label><input name="unit" required value="<?=e($editing['unit'] ?? '')?>" placeholder="kg, pcs, boxes"></div>
        </div>
        <label>Requested By</label>
        <input name="requested_by" required value="<?=e($editing['requested_by'] ?? '')?>" placeholder="Kitchen Staff">
        <label>Branch / Area</label>
        <input name="branch_area" required value="<?=e($editing['branch_area'] ?? '')?>" placeholder="Main Kitchen">
        <label>Status</label>
        <select name="status" required><?php foreach($statuses as $status): ?><option <?=selected($editing['status'] ?? 'Pending', $status)?>><?=e($status)?></option><?php endforeach; ?></select>
        <label>Remarks</label>
        <textarea name="remarks" rows="4" placeholder="Additional notes"><?=e($editing['remarks'] ?? '')?></textarea>
        <div class="form-actions">
          <button class="btn btn-primary" type="submit"><?= $editing ? 'Save Changes' : 'Add Record' ?></button>
          <?php if ($editing): ?><a class="btn btn-light" href="index.php">Cancel</a><?php endif; ?>
        </div>
      </form>
    </div>

    <div class="card">
      <h2>Inventory Records</h2>
      <form class="filters" method="get">
        <input name="search" value="<?=e($search)?>" placeholder="Search item, requester, area, remarks">
        <select name="category"><option value="">All Categories</option><?php foreach($categories as $cat): ?><option value="<?=e($cat)?>" <?=selected($filterCategory, $cat)?>><?=e($cat)?></option><?php endforeach; ?></select>
        <select name="status"><option value="">All Status</option><?php foreach($statuses as $status): ?><option value="<?=e($status)?>" <?=selected($filterStatus, $status)?>><?=e($status)?></option><?php endforeach; ?></select>
        <button class="btn btn-gold" type="submit">Filter</button>
      </form>
      <div class="table-wrap">
        <table>
          <thead><tr><th>ID</th><th>Item</th><th>Category</th><th>Quantity</th><th>Requested By</th><th>Area</th><th>Priority</th><th>Status</th><th>Remarks</th><th>Action</th></tr></thead>
          <tbody>
          <?php if (!$records): ?>
            <tr><td colspan="10"><div class="empty">No records found.</div></td></tr>
          <?php endif; ?>
          <?php foreach($records as $row): ?>
            <tr>
              <td>#<?=e($row['id'])?></td>
              <td><strong><?=e($row['ingredient_name'])?></strong><div class="small"><?=e($row['created_at'])?></div></td>
              <td><?=e($row['category'])?></td>
              <td><?=e($row['quantity'])?> <?=e($row['unit'])?></td>
              <td><?=e($row['requested_by'])?></td>
              <td><?=e($row['branch_area'])?></td>
              <td><?=e($row['priority'])?></td>
              <td><span class="badge <?=e(statusClass($row['status']))?>"><?=e($row['status'])?></span></td>
              <td><?=e($row['remarks'])?></td>
              <td class="row-actions">
                <a class="btn btn-light" href="?edit=<?=e($row['id'])?>">Edit</a>
                <form class="inline-form" method="post" onsubmit="return confirm('Delete this record?');">
                  <input type="hidden" name="csrf_token" value="<?=e(csrfToken())?>">
                  <input type="hidden" name="form_action" value="delete">
                  <input type="hidden" name="id" value="<?=e($row['id'])?>">
                  <button class="btn btn-danger" type="submit">Delete</button>
                </form>
              </td>
            </tr>
          <?php endforeach; ?>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</div>
<?php endif; ?>
</body>
</html>
