<?php
$conn = new mysqli("localhost", "root", "", "brailleplay");

if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

$first = isset($_POST['first_name']) ? $_POST['first_name'] : '';
$middle = isset($_POST['middle_name']) ? $_POST['middle_name'] : '';
$last = isset($_POST['last_name']) ? $_POST['last_name'] : '';
$age = isset($_POST['age']) ? $_POST['age'] : '';
$username = isset($_POST['username']) ? $_POST['username'] : '';
$password = isset($_POST['password']) ? $_POST['password'] : '';

// hash password
$hashed = password_hash($password, PASSWORD_DEFAULT);

// check if username exists
$check = $conn->prepare("SELECT student_id FROM student WHERE username=?");
$check->bind_param("s", $username);
$check->execute();
$check->store_result();

if ($check->num_rows > 0) {
    echo "Username already exists";
} else {
    $stmt = $conn->prepare("INSERT INTO student 
    (first_name, middle_name, last_name, age, username, password, Assessment) 
    VALUES (?, ?, ?, ?, ?, ?, 'Beginner')");

    $stmt->bind_param("sssiss", $first, $middle, $last, $age, $username, $hashed);

    if ($stmt->execute()) {
        echo "Success";
    } else {
        echo "Error: " . $stmt->error;
    }
}

$conn->close();
//lutang ka man pag d ka magregister ng student tapos aasa na may assessment
?>