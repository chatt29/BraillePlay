<?php
$conn = new mysqli("localhost", "root", "", "brailleplay");

$result = $conn->query("SELECT * FROM student");

$students = array();

while ($row = $result->fetch_assoc()) {
    $students[] = $row;
}

echo json_encode($students);

$conn->close();
//para lang to madali para sa admin yung paghanap sa students
?>
