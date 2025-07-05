-- Create the Students table
CREATE TABLE Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,       -- Auto-increment Id
    CourseId INT,                            -- Foreign Key for Course
    Name NVARCHAR(100),                      -- Student's Name
    Section NVARCHAR(50),                    -- Section of the student
    DOB DATE,                                -- Date of Birth
    Gender NVARCHAR(10),                     -- Gender
    Email NVARCHAR(100),                     -- Email Address
    Phone NVARCHAR(20),                      -- Phone Number
    Address NVARCHAR(255),                   -- Address
    ImageFileName NVARCHAR(255),             -- Image file name (for student picture)
    
    CONSTRAINT FK_Courses FOREIGN KEY (CourseId) REFERENCES Courses(Id)  -- Foreign Key Constraint to Courses table
);

-- Insert two sample records into the Students table
INSERT INTO Students (CourseId, Name, Section, DOB, Gender, Email, Phone, Address, ImageFileName)
VALUES
(1, 'John Doe', 'A', '2000-05-15', 'Male', 'johndoe@example.com', '123-456-7890', '123 Elm St, City', 'johndoe.jpg'),
(2, 'Jane Smith', 'B', '1999-09-25', 'Female', 'janesmith@example.com', '987-654-3210', '456 Oak St, City', 'janesmith.jpg');
