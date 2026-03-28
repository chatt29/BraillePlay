<?php
$conn = new mysqli("localhost", "root", "", "brailleplay");

$first = isset($_POST['first_name']) ? $_POST['first_name'] : '';
$middle = isset($_POST['middle_name']) ? $_POST['middle_name'] : '';
$last = isset($_POST['last_name']) ? $_POST['last_name'] : '';
$age = isset($_POST['age']) ? $_POST['age'] : '';
$assessment = isset($_POST['Assessment']) ? $_POST['Assessment'] : '';

$stmt = $conn->prepare("UPDATE student 
SET first_name=?, middle_name=?, last_name=?, age=?, Assessment=? 
WHERE student_id=?");

$stmt->bind_param("sssisi", $first, $middle, $last, $age, $assessment, $id);

if ($stmt->execute()) {
    echo "Updated";
} else {
    echo "Error: " . $stmt->error;
}

$conn->close();
// pagupdate ng admin sa students if ever may babaguhin especially sa assessment
?>